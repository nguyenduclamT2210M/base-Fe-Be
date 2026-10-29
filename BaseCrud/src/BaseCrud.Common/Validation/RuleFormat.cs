using System.Collections;
using System.Globalization;

namespace BaseCrud.Common.Validation;

/// <summary>Helper dùng chung cho các luật: kiểm tra rỗng, format số kiểu Việt Nam, ép kiểu số.</summary>
public static class RuleFormat
{
    // Tự tạo thay vì CultureInfo("vi-VN") để không phụ thuộc ICU của máy (Docker alpine, invariant mode...)
    private static readonly NumberFormatInfo Vn = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
    };

    public static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        ICollection c => c.Count == 0,
        _ => false,
    };

    /// <summary>1000000 → "1.000.000"; 0.5 → "0,5"</summary>
    public static string Number(decimal value) => value.ToString("#,##0.##########", Vn);

    public static string LowerFirst(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToLower(s[0], CultureInfo.InvariantCulture) + s[1..];

    /// <summary>Ép mọi kiểu số (và chuỗi số) về decimal. Không phải số → null.</summary>
    public static decimal? ToDecimal(object? value)
    {
        try
        {
            return value switch
            {
                null => null,
                decimal d => d,
                int or long or short or byte or sbyte or uint or ulong or ushort => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
                double db => double.IsFinite(db) ? (decimal)db : null,
                float f => float.IsFinite(f) ? (decimal)f : null,
                string s => decimal.TryParse(s.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var r) ? r : null,
                _ => null,
            };
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    /// <summary>Dung lượng tệp theo MB, 1 chữ số thập phân: 5452595 → "5.2"</summary>
    public static string Megabytes(long bytes) => (bytes / 1024d / 1024d).ToString("0.#", CultureInfo.InvariantCulture);
}
