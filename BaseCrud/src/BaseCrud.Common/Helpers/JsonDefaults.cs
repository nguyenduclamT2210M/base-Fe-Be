using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Json.Serialization;

namespace BaseCrud.Common.Helpers;

/// <summary>
/// Một cấu hình JSON dùng chung cho API và QueryBuilder
/// (để value trong filter được parse giống hệt body request).
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Create();

    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Apply(options);
        return options;
    }

    public static void Apply(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Giữ nguyên chữ tiếng Việt thay vì "ữ" (vẫn escape < > & ' để an toàn khi nhúng HTML)
        options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        // Enum nhận cả "active" lẫn 1
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        // Chặn ngày không tồn tại (31/02) với câu lỗi tiếng Việt; nhận thêm dạng dd/MM/yyyy
        options.Converters.Add(new StrictDateTimeConverter());
    }
}
