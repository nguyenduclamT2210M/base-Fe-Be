using BaseCrud.Common.Helpers;

namespace BaseCrud.Common.Validation;

/// <summary>
/// "Hôm nay" theo giờ Việt Nam (server có thể chạy UTC → 6h sáng VN vẫn là "hôm qua" theo UTC).
/// Test có thể gán UtcNow để cố định thời gian.
/// </summary>
public static class ValidationClock
{
    public static TimeZoneInfo TimeZone { get; set; } = FindVietnam();
    public static Func<DateTimeOffset> UtcNow { get; set; } = () => DateTimeOffset.UtcNow;

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow(), TimeZone).DateTime);

    private static TimeZoneInfo FindVietnam()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "UTC+07", "UTC+07");
    }
}

internal static class DateValue
{
    /// <summary>DateTime / DateTimeOffset / DateOnly / chuỗi ngày → DateOnly. Không đọc được → null.</summary>
    public static DateOnly? ToDate(object? value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        DateTimeOffset dto => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(dto, ValidationClock.TimeZone).DateTime),
        string s when DateParsing.TryParse(s, out var d, out _, out _) => d,
        _ => null,
    };

    public static DateTime? ToDateTime(object? value) => value switch
    {
        DateTime dt => dt,
        DateTimeOffset dto => dto.UtcDateTime,
        _ => ToDate(value)?.ToDateTime(TimeOnly.MinValue),
    };

    public static int Age(DateOnly birth, DateOnly today)
    {
        var age = today.Year - birth.Year;
        if (birth > today.AddYears(-age)) age--; // chưa tới sinh nhật năm nay
        return age;
    }
}

/// <summary>
/// Cho trường CHUỖI chứa ngày ("31/02/2026"). Trường kiểu DateTime đã được StrictDateTimeConverter
/// chặn ngày không tồn tại từ lúc đọc JSON.
/// </summary>
public sealed class DateAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
    {
        if (value is not string s) return null;
        if (DateParsing.TryParse(s, out _, out var hasTime, out var error) && !hasTime) return null;
        return $"{ctx.Label}: {RuleFormat.LowerFirst(error ?? DateParsing.FormatError)}";
    }
}

public sealed class NotFutureDateAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
        => DateValue.ToDate(value) is DateOnly d && d > ValidationClock.Today
            ? $"{ctx.Label} không được là ngày trong tương lai"
            : null;
}

public sealed class NotPastDateAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
        => DateValue.ToDate(value) is DateOnly d && d < ValidationClock.Today
            ? $"{ctx.Label} không được là ngày trong quá khứ"
            : null;
}

/// <summary>Đặt trên trường ngày sinh: Phải đủ 18 tuổi trở lên.</summary>
public sealed class MinAgeAttribute(int years) : RuleAttribute
{
    public int Years { get; } = years;

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (DateValue.ToDate(value) is not DateOnly birth) return null;
        var age = DateValue.Age(birth, ValidationClock.Today);
        if (age >= Years) return null;
        return age >= 0
            ? $"Phải đủ {Years} tuổi trở lên (hiện tại {age} tuổi)"
            : $"Phải đủ {Years} tuổi trở lên";
    }
}

public sealed class MaxAgeAttribute(int years) : RuleAttribute
{
    public int Years { get; } = years;

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (DateValue.ToDate(value) is not DateOnly birth) return null;
        var age = DateValue.Age(birth, ValidationClock.Today);
        return age > Years ? $"Không được quá {Years} tuổi (hiện tại {age} tuổi)" : null;
    }
}

/// <summary>
/// [DateAfter(nameof(StartDate))] → "Ngày kết thúc phải sau ngày bắt đầu".
/// AllowEqual = true → cho phép trùng ngày. Trường kia trống → bỏ qua.
/// </summary>
public sealed class DateAfterAttribute(string otherProperty) : RuleAttribute
{
    public string OtherProperty { get; } = otherProperty;
    public bool AllowEqual { get; set; }

    protected override string? Check(object? value, RuleContext ctx)
    {
        var current = DateValue.ToDateTime(value);
        var other = DateValue.ToDateTime(ctx.GetValue(OtherProperty));
        if (current is null || other is null) return null;

        var ok = AllowEqual ? current >= other : current > other;
        if (ok) return null;
        var otherLabel = RuleFormat.LowerFirst(ctx.GetLabel(OtherProperty));
        return AllowEqual
            ? $"{ctx.Label} không được trước {otherLabel}"
            : $"{ctx.Label} phải sau {otherLabel}";
    }
}
