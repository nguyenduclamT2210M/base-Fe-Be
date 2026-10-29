using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using BaseCrud.Common.Attributes;
using BaseCrud.Common.Entities;
using BaseCrud.Common.Exceptions;

namespace BaseCrud.DL.Metadata;

/// <summary>
/// Đọc attribute của entity bằng reflection MỘT LẦN rồi cache theo Type.
/// Đây cũng là whitelist tên cột: mọi tên field client gửi lên phải khớp ở đây
/// thì mới được ghép vào SQL → chặn SQL injection qua tên cột.
/// </summary>
public sealed class EntityMetadata
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> Cache = new();

    private readonly Dictionary<string, ColumnInfo> _byProperty;
    private readonly Dictionary<string, ColumnInfo> _byColumn;
    private readonly ColumnInfo? _key;

    public Type EntityType { get; }

    /// <summary>[schema].[table] đã quote.</summary>
    public string TableName { get; }

    public IReadOnlyList<ColumnInfo> Columns { get; }
    public ColumnInfo Key => _key ?? throw new InvalidOperationException($"{EntityType.Name} không có khóa chính (Id hoặc [Key]).");
    public ColumnInfo? RowVersion { get; }
    public ColumnInfo? SoftDeleteColumn { get; }
    public bool IsSoftDelete => SoftDeleteColumn is not null;
    public bool IsAuditable { get; }

    /// <summary>Cột sort mặc định khi client không truyền sort (CreatedAt nếu có).</summary>
    public ColumnInfo? DefaultSortColumn { get; }

    /// <summary>"[Id], [Code], ..." dùng cho SELECT.</summary>
    public string SelectList { get; }

    /// <summary>"INSERTED.[Id], INSERTED.[Code], ..." dùng cho OUTPUT.</summary>
    public string OutputList { get; }

    public static EntityMetadata For<T>() => For(typeof(T));
    public static EntityMetadata For(Type type) => Cache.GetOrAdd(type, Build);

    private EntityMetadata(Type type, string tableName, List<ColumnInfo> columns, bool isAuditable, bool isSoftDelete)
    {
        EntityType = type;
        TableName = tableName;
        Columns = columns;
        IsAuditable = isAuditable;

        _byProperty = columns.ToDictionary(c => c.PropertyName, StringComparer.OrdinalIgnoreCase);
        _byColumn = columns.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
        _key = columns.FirstOrDefault(c => c.IsKey);
        RowVersion = columns.FirstOrDefault(c => c.IsRowVersion);
        SoftDeleteColumn = isSoftDelete ? FindByProperty(nameof(ISoftDelete.IsDeleted)) : null;
        DefaultSortColumn = isAuditable ? FindByProperty(nameof(IAuditable.CreatedAt)) : null;

        SelectList = string.Join(", ", columns.Select(c => c.QuotedName));
        OutputList = string.Join(", ", columns.Select(c => "INSERTED." + c.QuotedName));
    }

    public ColumnInfo? FindByProperty(string propertyName) => _byProperty.GetValueOrDefault(propertyName);
    public ColumnInfo? FindByColumnName(string columnName) => _byColumn.GetValueOrDefault(columnName);

    /// <summary>Dùng nội bộ (code server), cho phép cả cột [NotQueryable].</summary>
    public ColumnInfo GetColumnOrThrow(string propertyName)
        => FindByProperty(propertyName)
           ?? throw new ValidateException("field", $"Trường '{propertyName}' không tồn tại.");

    /// <summary>Dùng cho input từ client: filter / sort / fields.</summary>
    public ColumnInfo GetQueryableOrThrow(string propertyName)
    {
        var col = FindByProperty(propertyName);
        if (col is null || !col.IsQueryable)
            throw new ValidateException("field", $"Trường '{propertyName}' không tồn tại hoặc không được phép truy vấn.");
        return col;
    }

    public static string Quote(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

    private static EntityMetadata Build(Type type)
    {
        var table = type.GetCustomAttribute<TableAttribute>();
        var tableName = $"{Quote(table?.Schema ?? "dbo")}.{Quote(table?.Name ?? type.Name)}";

        var isAuditable = typeof(IAuditable).IsAssignableFrom(type);
        var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(type);
        var hasRowVersion = typeof(IHasRowVersion).IsAssignableFrom(type);

        var nonUpdatable = new HashSet<string>(StringComparer.Ordinal);
        if (isAuditable)
        {
            nonUpdatable.Add(nameof(IAuditable.CreatedAt));
            nonUpdatable.Add(nameof(IAuditable.CreatedBy));
        }
        if (isSoftDelete) nonUpdatable.Add(nameof(ISoftDelete.IsDeleted));

        var columns = new List<ColumnInfo>();
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite || p.GetIndexParameters().Length > 0) continue;
            if (p.IsDefined(typeof(NotMappedAttribute), true)) continue;

            var clrType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            if (!SqlTypeMap.IsSupported(clrType)) continue;

            var colAttr = p.GetCustomAttribute<ColumnAttribute>();
            var colName = colAttr?.Name ?? p.Name;
            var isKey = p.IsDefined(typeof(KeyAttribute), true) || p.Name == nameof(BaseEntity.Id);
            var isRowVersion = p.IsDefined(typeof(TimestampAttribute), true)
                               || (hasRowVersion && p.Name == nameof(IHasRowVersion.RowVersion));

            var (dbType, size) = isRowVersion
                ? (SqlDbType.Binary, (int?)8)
                : SqlTypeMap.Resolve(clrType, colAttr?.TypeName, p);

            columns.Add(new ColumnInfo
            {
                Property = p,
                PropertyName = p.Name,
                ColumnName = colName,
                QuotedName = Quote(colName),
                ClrType = clrType,
                SqlDbType = dbType,
                Size = size,
                IsKey = isKey,
                IsRowVersion = isRowVersion,
                IsNullable = !p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) is not null,
                IsUpdatable = !isKey && !isRowVersion && !nonUpdatable.Contains(p.Name),
                IsQueryable = !p.IsDefined(typeof(NotQueryableAttribute), true),
                Getter = BuildGetter(type, p),
            });
        }

        return new EntityMetadata(type, tableName, columns, isAuditable, isSoftDelete);
    }

    private static Func<object, object?> BuildGetter(Type type, PropertyInfo property)
    {
        var obj = Expression.Parameter(typeof(object), "o");
        var body = Expression.Convert(Expression.Property(Expression.Convert(obj, type), property), typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, obj).Compile();
    }
}
