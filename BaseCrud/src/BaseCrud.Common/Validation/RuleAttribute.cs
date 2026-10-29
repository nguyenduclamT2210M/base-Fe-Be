using System.Reflection;

namespace BaseCrud.Common.Validation;

/// <summary>Tên hiển thị của trường trong câu lỗi. Không khai báo → dùng tên property.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LabelAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

/// <summary>
/// Gốc của mọi luật validate.
/// - Luật "có mặt" (Required, RequiredSelect, Accepted, SameAs) luôn chạy, và chạy trước.
/// - Các luật còn lại BỎ QUA khi giá trị rỗng → trường không bắt buộc thì để trống vẫn hợp lệ.
/// - Mỗi trường chỉ báo lỗi ĐẦU TIÊN (theo thứ tự khai báo).
/// - Message tùy biến: dùng {label} để chèn tên trường, vd Message = "{label} sai rồi".
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public abstract class RuleAttribute : Attribute
{
    public string? Message { get; set; }

    public virtual bool IsPresenceRule => false;

    public string? Validate(object? value, RuleContext ctx)
    {
        if (!IsPresenceRule && RuleFormat.IsEmpty(value)) return null;
        var error = Check(value, ctx);
        if (error is null) return null;
        return Message is null ? error : Message.Replace("{label}", ctx.Label);
    }

    /// <summary>Trả về null nếu hợp lệ, ngược lại là câu lỗi.</summary>
    protected abstract string? Check(object? value, RuleContext ctx);
}

public sealed class RuleContext
{
    private readonly RuleValidator.TypeRules _type;

    internal RuleContext(object instance, RuleValidator.PropertyRules property, RuleValidator.TypeRules type)
    {
        Instance = instance;
        Property = property.Property;
        Label = property.Label;
        _type = type;
    }

    public object Instance { get; }
    public PropertyInfo Property { get; }

    /// <summary>"Họ tên"</summary>
    public string Label { get; }

    /// <summary>"họ tên" — dùng khi tên trường đứng giữa câu.</summary>
    public string LabelLower => RuleFormat.LowerFirst(Label);

    /// <summary>Đọc giá trị trường khác (dùng cho SameAs, DateAfter).</summary>
    public object? GetValue(string propertyName) => _type.Get(propertyName).Property.GetValue(Instance);

    public string GetLabel(string propertyName) => _type.Get(propertyName).Label;
}
