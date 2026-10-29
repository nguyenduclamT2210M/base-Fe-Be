using System.Text.Json;
using System.Text.RegularExpressions;
using BaseCrud.Common.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BaseCrud.API.Infrastructure;

/// <summary>
/// Lỗi xảy ra lúc ĐỌC request (trước khi tới BL): JSON sai cú pháp, sai kiểu ("abc" vào trường số),
/// ngày không tồn tại, body rỗng... → câu lỗi tiếng Việt có tên trường, key là tên trường camelCase.
/// </summary>
public static partial class ModelStateTranslator
{
    [GeneratedRegex(@"could not be converted to (?:System\.Nullable`1\[)?(?:System\.)?([\w.]+)")]
    private static partial Regex TypeRegex();

    public static Dictionary<string, string[]> Translate(ActionContext ctx)
    {
        // Kiểu DTO của body/form → tra [Label] để câu lỗi có tên trường ("Lương phải là số")
        var modelType = ctx.ActionDescriptor.Parameters
            .FirstOrDefault(p => p.BindingInfo?.BindingSource == BindingSource.Body
                                 || p.BindingInfo?.BindingSource == BindingSource.Form)
            ?.ParameterType;

        var result = new Dictionary<string, string[]>();
        foreach (var (rawKey, entry) in ctx.ModelState)
        {
            if (entry.Errors.Count == 0) continue;
            var key = NormalizeKey(rawKey);
            var label = modelType is not null && !key.Contains('.') && !key.Contains('[')
                ? RuleValidator.TryGetLabel(modelType, key)
                : null;
            result[key] = new[] { Translate(entry.Errors[0], label) };
        }
        return result;
    }

    // "$.dateOfBirth" → "dateOfBirth", "$.items[0].name" → "items[0].name", "$" / "" → "body"
    private static string NormalizeKey(string key)
    {
        var k = key.StartsWith("$.") ? key[2..] : key;
        if (k is "" or "$") return "body";
        return JsonNamingPolicy.CamelCase.ConvertName(k);
    }

    private static string Translate(ModelError error, string? label)
    {
        var msg = string.IsNullOrEmpty(error.ErrorMessage) ? error.Exception?.Message ?? "" : error.ErrorMessage;

        if (msg.StartsWith("A non-empty request body is required", StringComparison.Ordinal))
            return "Thiếu dữ liệu gửi lên";

        var m = TypeRegex().Match(msg);
        if (m.Success)
        {
            var kind = m.Groups[1].Value switch
            {
                "Int16" or "Int32" or "Int64" or "Byte" => "số nguyên",
                "Decimal" or "Double" or "Single" => "số",
                "Boolean" => "true hoặc false",
                "Guid" => "mã định danh (GUID)",
                "DateTime" or "DateTimeOffset" or "DateOnly" => "ngày",
                _ => null,
            };
            var subject = label ?? "Giá trị";
            return kind is null ? $"{subject} không đúng kiểu dữ liệu" : $"{subject} phải là {kind}";
        }

        if (msg.Contains("is an invalid start of a value", StringComparison.Ordinal)
            || msg.Contains("is invalid after a value", StringComparison.Ordinal)
            || msg.Contains("Expected", StringComparison.Ordinal))
            return "Dữ liệu JSON sai cú pháp";

        if (string.IsNullOrEmpty(msg)) return label is null ? "Giá trị không hợp lệ" : $"{label} không hợp lệ";

        // Câu lỗi tiếng Việt do StrictDateTimeConverter đặt ("Ngày 31/02/2000 không tồn tại") → thêm tên trường
        return label is null ? msg : $"{label}: {RuleFormat.LowerFirst(msg)}";
    }
}
