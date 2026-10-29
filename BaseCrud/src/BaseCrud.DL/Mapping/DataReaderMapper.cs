using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using BaseCrud.DL.Metadata;

namespace BaseCrud.DL.Mapping;

/// <summary>
/// Map DbDataReader → object bằng delegate compile từ Expression Tree.
/// Delegate được cache theo (Type + danh sách cột của result set), nên lần đầu chậm
/// (compile), các lần sau nhanh gần bằng code viết tay.
///
/// Delegate sinh ra tương đương:
///   r => new T {
///       Id   = r.IsDBNull(0) ? default : r.GetFieldValue&lt;Guid&gt;(0),
///       Status = r.IsDBNull(5) ? default : (CustomerStatus) r.GetFieldValue&lt;int&gt;(5),
///       ... }
/// </summary>
public static class DataReaderMapper
{
    private static readonly ConcurrentDictionary<string, Delegate> Cache = new();

    private static readonly MethodInfo IsDbNullMethod =
        typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;

    private static readonly MethodInfo GetFieldValueMethod =
        typeof(DbDataReader).GetMethods()
            .Single(m => m.Name == nameof(DbDataReader.GetFieldValue)
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length == 1);

    public static async Task<List<T>> ReadAllAsync<T>(DbDataReader reader, EntityMetadata meta, CancellationToken ct)
        where T : new()
    {
        var list = new List<T>();
        if (reader.FieldCount == 0) return list;

        var map = GetMapper<T>(reader, meta);
        while (await reader.ReadAsync(ct))
            list.Add(map(reader));
        return list;
    }

    public static Func<DbDataReader, T> GetMapper<T>(DbDataReader reader, EntityMetadata meta) where T : new()
    {
        var names = new string[reader.FieldCount];
        for (var i = 0; i < names.Length; i++) names[i] = reader.GetName(i);

        var key = typeof(T).FullName + ":" + string.Join('|', names);
        return (Func<DbDataReader, T>)Cache.GetOrAdd(key, _ => Build<T>(names, meta));
    }

    private static Func<DbDataReader, T> Build<T>(string[] names, EntityMetadata meta) where T : new()
    {
        var reader = Expression.Parameter(typeof(DbDataReader), "r");
        var bindings = new List<MemberBinding>();

        for (var i = 0; i < names.Length; i++)
        {
            var col = meta.FindByColumnName(names[i]);
            if (col is null) continue; // cột thừa trong result set (vd TotalCount) → bỏ qua

            var index = Expression.Constant(i);
            var propType = col.Property.PropertyType;
            var readType = col.ClrType.IsEnum ? Enum.GetUnderlyingType(col.ClrType) : col.ClrType;

            Expression read = Expression.Call(reader, GetFieldValueMethod.MakeGenericMethod(readType), index);
            if (readType != col.ClrType) read = Expression.Convert(read, col.ClrType);   // int → enum
            if (col.ClrType != propType) read = Expression.Convert(read, propType);      // T → T?

            var value = Expression.Condition(
                Expression.Call(reader, IsDbNullMethod, index),
                Expression.Default(propType),
                read);

            bindings.Add(Expression.Bind(col.Property, value));
        }

        var body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
        return Expression.Lambda<Func<DbDataReader, T>>(body, reader).Compile();
    }
}
