namespace BaseCrud.Common.Validation;

/// <summary>
/// Mật khẩu cần thêm: chữ hoa, ký tự đặc biệt — chỉ liệt kê điều kiện CÒN THIẾU.
/// Mặc định: ≥ 8 ký tự, có chữ hoa, chữ thường, chữ số, ký tự đặc biệt. Tắt bớt bằng property.
/// </summary>
public sealed class PasswordAttribute : RuleAttribute
{
    public int MinLength { get; set; } = 8;
    public bool RequireUpper { get; set; } = true;
    public bool RequireLower { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecial { get; set; } = true;

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (value is not string s) return null;
        var missing = new List<string>();
        if (s.Length < MinLength) missing.Add($"ít nhất {MinLength} ký tự (hiện tại {s.Length})");
        if (RequireUpper && !s.Any(char.IsUpper)) missing.Add("chữ hoa");
        if (RequireLower && !s.Any(char.IsLower)) missing.Add("chữ thường");
        if (RequireDigit && !s.Any(char.IsDigit)) missing.Add("chữ số");
        if (RequireSpecial && !s.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))) missing.Add("ký tự đặc biệt");
        return missing.Count == 0 ? null : $"{ctx.Label} cần thêm: {string.Join(", ", missing)}";
    }
}

/// <summary>
/// Phải trùng với trường khác: [SameAs(nameof(Password))] → "Xác nhận mật khẩu không khớp với mật khẩu".
/// Là luật "có mặt": một bên trống, một bên có giá trị cũng tính là không khớp.
/// </summary>
public sealed class SameAsAttribute(string otherProperty) : RuleAttribute
{
    public string OtherProperty { get; } = otherProperty;

    public override bool IsPresenceRule => true;

    protected override string? Check(object? value, RuleContext ctx)
    {
        var other = ctx.GetValue(OtherProperty);
        var a = value?.ToString() ?? string.Empty;
        var b = other?.ToString() ?? string.Empty;
        return string.Equals(a, b, StringComparison.Ordinal)
            ? null
            : $"{ctx.Label} không khớp với {RuleFormat.LowerFirst(ctx.GetLabel(OtherProperty))}";
    }
}
