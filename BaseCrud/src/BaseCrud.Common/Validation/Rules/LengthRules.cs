namespace BaseCrud.Common.Validation;

// Độ dài tính theo số ký tự của chuỗi (đúng cách SQL Server đếm NVARCHAR(n)).
// MaxLength / LengthBetween / ExactLength còn được DL đọc để set Size cho SqlParameter.

public sealed class MinLengthAttribute(int min) : RuleAttribute
{
    public int Min { get; } = min;

    protected override string? Check(object? value, RuleContext ctx)
        => value is string s && s.Length < Min
            ? $"{ctx.Label} phải có ít nhất {Min} ký tự (hiện tại {s.Length})"
            : null;
}

public sealed class MaxLengthAttribute(int max) : RuleAttribute
{
    public int Max { get; } = max;

    protected override string? Check(object? value, RuleContext ctx)
        => value is string s && s.Length > Max
            ? $"{ctx.Label} không được vượt quá {Max} ký tự (hiện tại {s.Length})"
            : null;
}

public sealed class ExactLengthAttribute(int length) : RuleAttribute
{
    public int Length { get; } = length;

    protected override string? Check(object? value, RuleContext ctx)
        => value is string s && s.Length != Length
            ? $"{ctx.Label} phải có đúng {Length} ký tự (hiện tại {s.Length})"
            : null;
}

public sealed class LengthBetweenAttribute(int min, int max) : RuleAttribute
{
    public int Min { get; } = min;
    public int Max { get; } = max;

    protected override string? Check(object? value, RuleContext ctx)
        => value is string s && (s.Length < Min || s.Length > Max)
            ? $"{ctx.Label} phải từ {Min} đến {Max} ký tự (hiện tại {s.Length})"
            : null;
}
