using System.Data;
using BaseCrud.DL.Metadata;
using Microsoft.Data.SqlClient;

namespace BaseCrud.DL.Infrastructure;

/// <summary>Tạo SqlParameter có kiểu/size tường minh (không để SqlClient tự đoán).</summary>
public static class SqlParam
{
    public static SqlParameter Create(string name, object? value, ColumnInfo column)
    {
        var p = new SqlParameter(name, column.SqlDbType);
        if (column.Size is int size)
        {
            var len = value switch
            {
                string s => s.Length,
                byte[] b => b.Length,
                _ => 0,
            };
            // Giá trị dài hơn size khai báo (vd pattern LIKE có thêm % và ký tự escape)
            // → dùng MAX, tránh SqlClient cắt cụt âm thầm.
            p.Size = size > 0 && len > size ? -1 : size;
        }
        p.Value = ToDbValue(value);
        return p;
    }

    public static SqlParameter Create(string name, object? value, SqlDbType type)
        => new(name, type) { Value = ToDbValue(value) };

    public static object ToDbValue(object? value) => value switch
    {
        null => DBNull.Value,
        Enum e => Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType())),
        _ => value,
    };
}
