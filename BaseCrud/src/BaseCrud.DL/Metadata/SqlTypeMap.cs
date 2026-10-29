using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using V = BaseCrud.Common.Validation;

namespace BaseCrud.DL.Metadata;

/// <summary>Map kiểu C# → SqlDbType. Có thể override bằng [Column(TypeName = "...")].</summary>
internal static class SqlTypeMap
{
    private static readonly Dictionary<Type, SqlDbType> Map = new()
    {
        [typeof(Guid)] = SqlDbType.UniqueIdentifier,
        [typeof(string)] = SqlDbType.NVarChar,
        [typeof(int)] = SqlDbType.Int,
        [typeof(long)] = SqlDbType.BigInt,
        [typeof(short)] = SqlDbType.SmallInt,
        [typeof(byte)] = SqlDbType.TinyInt,
        [typeof(bool)] = SqlDbType.Bit,
        [typeof(decimal)] = SqlDbType.Decimal,
        [typeof(double)] = SqlDbType.Float,
        [typeof(float)] = SqlDbType.Real,
        [typeof(DateTime)] = SqlDbType.DateTime2,
        [typeof(DateTimeOffset)] = SqlDbType.DateTimeOffset,
        [typeof(TimeSpan)] = SqlDbType.Time,
        [typeof(byte[])] = SqlDbType.VarBinary,
    };

    /// <summary>Kiểu không hỗ trợ (class lồng, List...) sẽ bị bỏ qua, coi như không phải cột.</summary>
    public static bool IsSupported(Type clrType) => clrType.IsEnum || Map.ContainsKey(clrType);

    public static (SqlDbType Type, int? Size) Resolve(Type clrType, string? columnTypeName, PropertyInfo property)
    {
        var baseType = clrType.IsEnum ? Enum.GetUnderlyingType(clrType) : clrType;
        var type = Map[baseType];

        if (!string.IsNullOrWhiteSpace(columnTypeName))
        {
            var tn = columnTypeName.Trim().ToLowerInvariant();
            var paren = tn.IndexOf('(');
            var name = paren >= 0 ? tn[..paren].Trim() : tn;
            type = name switch
            {
                "varchar" or "char" or "text" => SqlDbType.VarChar,
                "nvarchar" or "nchar" or "ntext" => SqlDbType.NVarChar,
                "date" => SqlDbType.Date,
                "datetime" => SqlDbType.DateTime,
                "smalldatetime" => SqlDbType.SmallDateTime,
                "datetime2" => SqlDbType.DateTime2,
                "money" => SqlDbType.Money,
                _ => type,
            };
        }

        int? size = null;
        if (type is SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.VarBinary)
        {
            var len = property.GetCustomAttribute<V.MaxLengthAttribute>()?.Max
                      ?? property.GetCustomAttribute<V.LengthBetweenAttribute>()?.Max
                      ?? property.GetCustomAttribute<V.ExactLengthAttribute>()?.Length
                      ?? property.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength
                      ?? property.GetCustomAttribute<MaxLengthAttribute>()?.Length;
            // Có độ dài → parameter cố định size → SQL Server tái sử dụng execution plan.
            // Không khai báo → MAX (-1) để không bao giờ cắt cụt dữ liệu.
            size = len is > 0 ? len : -1;
        }

        return (type, size);
    }
}
