namespace BaseCrud.Common.Validation;

// Áp dụng cho property kiểu số (int, decimal, double...) hoặc chuỗi chứa số.
// Tham số attribute dùng double vì C# không cho decimal làm tham số attribute;
// so sánh vẫn chạy bằng decimal nên không lệch số.
// Property kiểu số mà client gửi "abc" → lỗi ngay lúc đọc JSON (xem Program.cs), không tới đây.

/// <summary>Chuỗi phải là số: "12.5" hợp lệ, "12a" không.</summary>
public sealed class NumberAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
        => RuleFormat.ToDecimal(value) is null ? $"{ctx.Label} phải là số" : null;
}

public sealed class IntegerAttribute : RuleAttribute
{
    protected override string? Check(object? value, RuleContext ctx)
        => RuleFormat.ToDecimal(value) is decimal d && d == decimal.Truncate(d)
            ? null
            : $"{ctx.Label} phải là số nguyên";
}

public sealed class MinAttribute(double min) : RuleAttribute
{
    public decimal Min { get; } = (decimal)min;

    protected override string? Check(object? value, RuleContext ctx)
        => RuleFormat.ToDecimal(value) is decimal d && d < Min
            ? $"{ctx.Label} không được nhỏ hơn {RuleFormat.Number(Min)}"
            : null;
}

public sealed class MaxAttribute(double max) : RuleAttribute
{
    public decimal Max { get; } = (decimal)max;

    protected override string? Check(object? value, RuleContext ctx)
        => RuleFormat.ToDecimal(value) is decimal d && d > Max
            ? $"{ctx.Label} không được lớn hơn {RuleFormat.Number(Max)}"
            : null;
}

/// <summary>Lương phải trong khoảng 1.000.000 đến 500.000.000</summary>
public sealed class BetweenAttribute(double min, double max) : RuleAttribute
{
    public decimal Min { get; } = (decimal)min;
    public decimal Max { get; } = (decimal)max;

    protected override string? Check(object? value, RuleContext ctx)
        => RuleFormat.ToDecimal(value) is decimal d && (d < Min || d > Max)
            ? $"{ctx.Label} phải trong khoảng {RuleFormat.Number(Min)} đến {RuleFormat.Number(Max)}"
            : null;
}

/// <summary>Tối đa n chữ số thập phân. Nên khớp scale của cột DECIMAL(p, s) trong DB.</summary>
public sealed class DecimalsAttribute(int places) : RuleAttribute
{
    public int Places { get; } = places;

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (RuleFormat.ToDecimal(value) is not decimal d) return null;
        var scaled = d * (decimal)Math.Pow(10, Places);
        return scaled == decimal.Truncate(scaled)
            ? null
            : Places == 0
                ? $"{ctx.Label} không được có phần thập phân"
                : $"{ctx.Label} chỉ được có tối đa {Places} chữ số thập phân";
    }
}
