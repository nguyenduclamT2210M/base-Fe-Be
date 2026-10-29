using System.Data;
using System.Reflection;

namespace BaseCrud.DL.Metadata;

/// <summary>Thông tin 1 cột, tính 1 lần lúc khởi tạo rồi cache.</summary>
public sealed class ColumnInfo
{
    public required PropertyInfo Property { get; init; }
    public required string PropertyName { get; init; }
    public required string ColumnName { get; init; }

    /// <summary>Tên cột đã bọc [ ] và escape ] → ]] — an toàn để ghép vào SQL.</summary>
    public required string QuotedName { get; init; }

    /// <summary>Kiểu property đã bỏ Nullable&lt;&gt; (enum giữ nguyên).</summary>
    public required Type ClrType { get; init; }

    public required SqlDbType SqlDbType { get; init; }

    /// <summary>Độ dài cho chuỗi/binary; -1 = MAX; null = không áp dụng.</summary>
    public int? Size { get; init; }

    public bool IsKey { get; init; }
    public bool IsRowVersion { get; init; }
    public bool IsNullable { get; init; }

    /// <summary>Được SET trong câu UPDATE (loại trừ Id, RowVersion, CreatedAt/By, IsDeleted).</summary>
    public bool IsUpdatable { get; init; }

    /// <summary>Client được filter/sort/chọn cột này hay không ([NotQueryable]).</summary>
    public bool IsQueryable { get; init; }

    /// <summary>Getter đã compile bằng Expression Tree, nhanh hơn PropertyInfo.GetValue.</summary>
    public required Func<object, object?> Getter { get; init; }
}
