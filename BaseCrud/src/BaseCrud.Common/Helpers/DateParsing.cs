using System.Globalization;
using System.Text.RegularExpressions;

namespace BaseCrud.Common.Helpers;

/// <summary>
/// Đọc ngày dạng "dd/MM/yyyy" hoặc "yyyy-MM-dd[THH:mm...]" và chẩn đoán lỗi cụ thể:
/// "31/02/2026" → "Ngày 31/02/2026 không tồn tại" (thay vì "sai định dạng" chung chung).
/// </summary>
public static partial class DateParsing
{
    public const string FormatError = "Ngày không đúng định dạng (dd/MM/yyyy hoặc yyyy-MM-dd)";

    [GeneratedRegex(@"^(\d{4})-(\d{1,2})-(\d{1,2})([T ].*)?$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex IsoRegex();

    [GeneratedRegex(@"^(\d{1,2})/(\d{1,2})/(\d{4})$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex VnRegex();

    /// <param name="hasTime">Chuỗi có phần giờ (chỉ phần ngày được kiểm tra ở hàm này).</param>
    public static bool TryParse(string? input, out DateOnly date, out bool hasTime, out string? error)
    {
        date = default;
        hasTime = false;
        error = null;
        var s = input?.Trim() ?? string.Empty;

        int y, m, d;
        Match match;
        if ((match = IsoRegex().Match(s)).Success)
        {
            y = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            m = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            d = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            hasTime = match.Groups[4].Success;
        }
        else if ((match = VnRegex().Match(s)).Success)
        {
            d = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            m = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            y = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        }
        else
        {
            error = FormatError;
            return false;
        }

        if (y < 1 || m < 1 || m > 12)
        {
            error = $"Tháng {m} không hợp lệ";
            return false;
        }
        if (d < 1 || d > DateTime.DaysInMonth(y, m))
        {
            error = $"Ngày {d:00}/{m:00}/{y:0000} không tồn tại";
            return false;
        }

        date = new DateOnly(y, m, d);
        return true;
    }
}
