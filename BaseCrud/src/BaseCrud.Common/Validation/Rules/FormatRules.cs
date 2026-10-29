using System.Text.RegularExpressions;

namespace BaseCrud.Common.Validation;

// Mọi Regex đều có timeout để chống ReDoS (chuỗi đầu vào cố tình làm regex chạy rất lâu).

public sealed partial class EmailAttribute : RuleAttribute
{
    // Đủ chặt cho thực tế: local@domain.tld, không khoảng trắng, TLD ≥ 2 ký tự.
    [GeneratedRegex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9\-]*[A-Za-z0-9])?)*\.[A-Za-z]{2,}$",
        RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex EmailRegex();

    protected override string? Check(object? value, RuleContext ctx)
        => value is string s && (s.Length > 254 || s.Contains("..") || !EmailRegex().IsMatch(s))
            ? $"{ctx.Label} không đúng định dạng (ví dụ: ten@congty.vn)"
            : null;
}

/// <summary>
/// Số di động Việt Nam 10 số. Cho phép khoảng trắng, dấu chấm, gạch ngang, ngoặc và tiền tố +84.
/// Báo riêng từng trường hợp theo thứ tự: có chữ/ký tự lạ → độ dài → không bắt đầu bằng 0 → sai đầu số.
/// Lưu ý: số máy bàn (02x) có 11 số, không dùng luật này.
/// </summary>
public sealed partial class PhoneAttribute : RuleAttribute
{
    /// <summary>Đầu số di động hợp lệ — sửa ở đây khi nhà mạng mở đầu số mới.</summary>
    public static HashSet<string> ValidPrefixes { get; } = new()
    {
        "032", "033", "034", "035", "036", "037", "038", "039",
        "052", "055", "056", "058", "059",
        "070", "076", "077", "078", "079",
        "081", "082", "083", "084", "085", "086", "087", "088", "089",
        "090", "091", "092", "093", "094", "096", "097", "098", "099",
    };

    [GeneratedRegex(@"[\s.\-()]", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex SeparatorRegex();

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (value is not string s) return null;
        var digits = SeparatorRegex().Replace(s.Trim(), "");
        if (digits.StartsWith("+84")) digits = "0" + digits[3..];

        if (digits.Any(char.IsLetter)) return $"{ctx.Label} không được chứa chữ cái";
        if (!digits.All(char.IsAsciiDigit)) return $"{ctx.Label} không được chứa ký tự đặc biệt";
        if (digits.Length != 10) return $"{ctx.Label} phải đủ 10 chữ số (hiện tại {digits.Length} số)";
        if (digits[0] != '0') return $"{ctx.Label} phải bắt đầu bằng số 0";
        if (!ValidPrefixes.Contains(digits[..3])) return $"{ctx.Label} có đầu số “{digits[..3]}” không hợp lệ";
        return null;
    }
}

public sealed class UrlAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
        => value is string s
           && !(Uri.TryCreate(s.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && uri.Host.Contains('.'))
            ? $"{ctx.Label} không phải địa chỉ web hợp lệ (ví dụ: https://congty.vn)"
            : null;
}

/// <summary>Mã (mã NV, mã KH...): chỉ chữ không dấu, chữ số, gạch ngang, gạch dưới.</summary>
public sealed partial class CodeAttribute : RuleAttribute
{
    [GeneratedRegex(@"^[A-Za-z0-9_\-]+$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex CodeRegex();

    protected override string? Check(object? value, RuleContext ctx)
        => value is string s && !CodeRegex().IsMatch(s)
            ? $"{ctx.Label} chỉ gồm chữ không dấu, chữ số, dấu gạch ngang (-) và gạch dưới (_)"
            : null;
}

/// <summary>Tên người: chữ cái (có dấu), khoảng trắng, dấu nháy đơn.</summary>
public sealed class PersonNameAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
    {
        if (value is not string s) return null;
        if (s.Any(char.IsDigit)) return $"{ctx.Label} không được chứa chữ số";
        // char.IsLetter + dấu tổ hợp (NonSpacingMark) để nhận cả chữ Việt gõ kiểu tổ hợp
        if (s.Any(c => !char.IsLetter(c) && c != ' ' && c != '\''
                       && char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark))
            return $"{ctx.Label} không được chứa ký tự đặc biệt";
        if (s.Contains("  ")) return $"{ctx.Label} không được có nhiều khoảng trắng liền nhau";
        return null;
    }
}

public sealed class NoWhitespaceAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
        => value is string s && s.Any(char.IsWhiteSpace) ? $"{ctx.Label} không được chứa khoảng trắng" : null;
}

/// <summary>
/// Regex tùy ý. Bắt buộc truyền Message (vì không thể đoán câu lỗi từ regex).
/// Ví dụ: [Pattern(@"^\d{10}(\d{3})?$", Message = "{label} phải gồm 10 hoặc 13 chữ số")]
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class PatternAttribute : RuleAttribute
{
    private readonly Regex _regex;

    public PatternAttribute(string pattern)
    {
        Pattern = pattern;
        _regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }

    public string Pattern { get; }

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (value is not string s) return null;
        try
        {
            return _regex.IsMatch(s) ? null : $"{ctx.Label} không đúng định dạng";
        }
        catch (RegexMatchTimeoutException)
        {
            return $"{ctx.Label} không đúng định dạng";
        }
    }
}
