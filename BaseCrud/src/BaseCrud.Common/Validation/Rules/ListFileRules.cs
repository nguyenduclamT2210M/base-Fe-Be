using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace BaseCrud.Common.Validation;

/// <summary>
/// Giá trị phải nằm trong danh sách: [OneOf("male", "female", "other")].
/// Trên property enum không truyền gì: [OneOf] → chặn số enum không khai báo (vd status = 99),
/// vì JSON cho phép gửi số bất kỳ vào enum.
/// </summary>
public sealed class OneOfAttribute(params object[] values) : RuleAttribute
{
    public object[] Values { get; } = values;

    protected override string? Check(object? value, RuleContext ctx)
    {
        if (value is null) return null;

        if (Values.Length == 0 && value is Enum e)
        {
            var type = e.GetType();
            if (Enum.IsDefined(type, e)) return null;
            var names = Enum.GetNames(type).Select(JsonNamingPolicy.CamelCase.ConvertName);
            return $"{ctx.Label} không hợp lệ. Chỉ chấp nhận: {string.Join(", ", names)}";
        }

        var items = value is string || value is not IEnumerable list ? new[] { value } : list.Cast<object?>();
        foreach (var item in items)
        {
            if (item is null || Values.Any(v => Same(v, item))) continue;
            return $"{ctx.Label} không hợp lệ. Chỉ chấp nhận: {string.Join(", ", Values)}";
        }
        return null;
    }

    private static bool Same(object allowed, object actual)
        => RuleFormat.ToDecimal(allowed) is decimal a && RuleFormat.ToDecimal(actual) is decimal b
            ? a == b
            : string.Equals(Convert.ToString(allowed, CultureInfo.InvariantCulture),
                            Convert.ToString(actual, CultureInfo.InvariantCulture), StringComparison.Ordinal);
}

/// <summary>
/// Đọc tên + dung lượng tệp theo kiểu "duck typing": object có property Length và FileName
/// (IFormFile của ASP.NET), hoặc byte[]. Nhờ vậy Common không phụ thuộc ASP.NET.
/// </summary>
internal static class FileValue
{
    private static readonly ConcurrentDictionary<Type, (PropertyInfo? Length, PropertyInfo? Name)> Props = new();

    public static IEnumerable<(string Name, long Size)> Read(object? value)
    {
        if (value is null) yield break;
        if (value is byte[] bytes)
        {
            yield return ("(dữ liệu)", bytes.Length);
            yield break;
        }
        var items = value is IEnumerable list and not string ? list.Cast<object?>() : new[] { value };
        foreach (var item in items)
        {
            if (item is null) continue;
            if (item is byte[] b) { yield return ("(dữ liệu)", b.Length); continue; }
            var (lengthProp, nameProp) = Props.GetOrAdd(item.GetType(), t =>
                (t.GetProperty("Length"), t.GetProperty("FileName") ?? t.GetProperty("Name")));
            var size = lengthProp?.GetValue(item) is { } l ? Convert.ToInt64(l, CultureInfo.InvariantCulture) : 0;
            var name = nameProp?.GetValue(item)?.ToString() ?? "(không tên)";
            yield return (Path.GetFileName(name), size);
        }
    }
}

/// <summary>Ảnh không được vượt quá 2 MB (tệp “a.png” nặng 5.2 MB). Áp dụng cho 1 tệp hoặc danh sách tệp.</summary>
public sealed class FileSizeAttribute(double maxMegabytes) : RuleAttribute
{
    public double MaxMegabytes { get; } = maxMegabytes;

    protected override string? Check(object? value, RuleContext ctx)
    {
        var maxBytes = (long)(MaxMegabytes * 1024 * 1024);
        foreach (var (name, size) in FileValue.Read(value))
        {
            if (size <= maxBytes) continue;
            return $"{ctx.Label} không được vượt quá {MaxMegabytes.ToString("0.##", CultureInfo.InvariantCulture)} MB "
                   + $"(tệp “{name}” nặng {RuleFormat.Megabytes(size)} MB)";
        }
        return null;
    }
}

/// <summary>
/// [FileType("jpg", "png")] — kiểm tra theo ĐUÔI tệp.
/// Đuôi tệp có thể bị đổi tên giả mạo; với tệp nhạy cảm nên kiểm tra thêm magic bytes khi lưu.
/// </summary>
public sealed class FileTypeAttribute(params string[] extensions) : RuleAttribute
{
    public string[] Extensions { get; } = extensions.Select(e => e.TrimStart('.').ToLowerInvariant()).ToArray();

    protected override string? Check(object? value, RuleContext ctx)
    {
        foreach (var (name, _) in FileValue.Read(value))
        {
            var ext = Path.GetExtension(name).TrimStart('.').ToLowerInvariant();
            if (Extensions.Contains(ext)) continue;
            return $"{ctx.Label} chỉ chấp nhận tệp: {string.Join(", ", Extensions)} (tệp “{name}” không hợp lệ)";
        }
        return null;
    }
}
