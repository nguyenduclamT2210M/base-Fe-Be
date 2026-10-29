using System.Collections;

namespace BaseCrud.Common.Validation;

/// <summary>Họ tên không được để trống. (null, chuỗi toàn khoảng trắng, danh sách rỗng)</summary>
public sealed class RequiredAttribute : RuleAttribute
{
    public override bool IsPresenceRule => true;

    protected override string? Check(object? value, RuleContext ctx)
        => RuleFormat.IsEmpty(value) ? $"{ctx.Label} không được để trống" : null;
}

/// <summary>
/// Vui lòng chọn phòng ban. Dùng cho dropdown/select:
/// ngoài null còn coi Guid.Empty và 0 là "chưa chọn".
/// </summary>
public sealed class RequiredSelectAttribute : RuleAttribute
{
    public override bool IsPresenceRule => true;

    protected override string? Check(object? value, RuleContext ctx)
    {
        var empty = value switch
        {
            Guid g => g == Guid.Empty,
            Enum => false,
            _ when RuleFormat.ToDecimal(value) is decimal d && value is not string => d == 0,
            _ => RuleFormat.IsEmpty(value),
        };
        return empty ? $"Vui lòng chọn {ctx.LabelLower}" : null;
    }
}

/// <summary>Checkbox bắt buộc tick: Vui lòng đồng ý điều khoản sử dụng.</summary>
public sealed class AcceptedAttribute : RuleAttribute
{
    public override bool IsPresenceRule => true;

    protected override string? Check(object? value, RuleContext ctx)
        => value is true ? null : $"Vui lòng đồng ý {ctx.LabelLower}";
}

/// <summary>Danh sách được chọn tối đa n mục.</summary>
public sealed class MaxItemsAttribute(int max) : RuleAttribute
{
    public int Max { get; } = max;

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (value is string || value is not IEnumerable list) return null;
        var count = list is ICollection c ? c.Count : list.Cast<object?>().Count();
        return count > Max ? $"{ctx.Label} chỉ được chọn tối đa {Max} mục (hiện tại {count})" : null;
    }
}
