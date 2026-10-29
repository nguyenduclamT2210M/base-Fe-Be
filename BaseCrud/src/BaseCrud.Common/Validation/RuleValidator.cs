using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using BaseCrud.Common.Exceptions;

namespace BaseCrud.Common.Validation;

/// <summary>
/// Chạy các RuleAttribute trên một object (entity hoặc DTO).
/// Danh sách luật đọc bằng reflection 1 lần / Type rồi cache.
/// Unique KHÔNG chạy ở đây (cần DB) — BaseBL xử lý.
/// </summary>
public static class RuleValidator
{
    private static readonly ConcurrentDictionary<Type, TypeRules> Cache = new();

    /// <summary>Trả về { "fieldCamelCase": ["câu lỗi"] }. Rỗng = hợp lệ.</summary>
    public static Dictionary<string, string[]> Validate(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var type = For(instance.GetType());
        var errors = new Dictionary<string, string[]>();

        foreach (var p in type.All)
        {
            if (p.Rules.Length == 0) continue;
            var value = p.Property.GetValue(instance);
            var ctx = new RuleContext(instance, p, type);
            foreach (var rule in p.Rules)
            {
                var error = rule.Validate(value, ctx);
                if (error is null) continue;
                errors[p.Key] = new[] { error };
                break; // mỗi trường chỉ báo lỗi đầu tiên
            }
        }
        return errors;
    }

    public static void ValidateOrThrow(object instance)
    {
        var errors = Validate(instance);
        if (errors.Count > 0) throw new ValidateException(errors);
    }

    public static IReadOnlyList<UniqueField> GetUniqueFields(Type type) => For(type).UniqueFields;

    public static string GetLabel(Type type, string propertyName) => For(type).Get(propertyName).Label;

    /// <summary>Tìm label theo tên property (không phân biệt hoa thường). Không có → null.</summary>
    public static string? TryGetLabel(Type type, string propertyName) => For(type).Find(propertyName)?.Label;

    internal static TypeRules For(Type type) => Cache.GetOrAdd(type, t => new TypeRules(t));

    internal sealed class PropertyRules
    {
        public required PropertyInfo Property { get; init; }
        public required string Label { get; init; }
        public required string Key { get; init; }
        public required RuleAttribute[] Rules { get; init; }
    }

    internal sealed class TypeRules
    {
        private readonly Dictionary<string, PropertyRules> _byName;

        public List<PropertyRules> All { get; }
        public List<UniqueField> UniqueFields { get; }

        public TypeRules(Type type)
        {
            All = new List<PropertyRules>();
            UniqueFields = new List<UniqueField>();

            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                var label = p.GetCustomAttribute<LabelAttribute>()?.Text ?? p.Name;

                // Luật "có mặt" chạy trước, sau đó theo thứ tự khai báo
                var rules = p.GetCustomAttributes<RuleAttribute>(true)
                    .Select((r, i) => (r, i))
                    .OrderBy(x => x.r.IsPresenceRule ? 0 : 1).ThenBy(x => x.i)
                    .Select(x => x.r)
                    .ToArray();

                All.Add(new PropertyRules
                {
                    Property = p,
                    Label = label,
                    Key = JsonNamingPolicy.CamelCase.ConvertName(p.Name),
                    Rules = rules,
                });

                if (p.GetCustomAttribute<UniqueAttribute>() is { } u)
                    UniqueFields.Add(new UniqueField(p, label, u.Message));
            }

            _byName = All.ToDictionary(x => x.Property.Name, StringComparer.OrdinalIgnoreCase);
        }

        public PropertyRules? Find(string name) => _byName.GetValueOrDefault(name);

        public PropertyRules Get(string name)
            => _byName.TryGetValue(name, out var p)
                ? p
                : throw new InvalidOperationException($"Không tìm thấy property '{name}' — kiểm tra lại nameof(...) trong attribute.");
    }
}

/// <summary>Đánh dấu trường không được trùng (kiểm tra bằng DB ở BaseBL và endpoint check-unique).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueAttribute : Attribute
{
    /// <summary>Tùy biến câu lỗi: {label}, {value}.</summary>
    public string? Message { get; set; }
}

public sealed class UniqueField(PropertyInfo property, string label, string? message)
{
    public PropertyInfo Property { get; } = property;
    public string Label { get; } = label;

    public object? GetValue(object instance) => Property.GetValue(instance);

    /// <summary>Mã nhân viên “NV001” đã tồn tại</summary>
    public string FormatMessage(object? value)
        => (message ?? "{label} “{value}” đã tồn tại").Replace("{label}", Label).Replace("{value}", value?.ToString());
}

public sealed class UniqueCheckResult
{
    public bool IsTaken { get; init; }
    public string? Message { get; init; }
}
