using System.Text.Json;
using System.Text.Json.Serialization;

namespace BaseCrud.Common.Helpers;

/// <summary>
/// Đọc DateTime từ JSON: nhận ISO 8601 ("2026-10-29", "2026-10-29T08:00:00Z") và "dd/MM/yyyy".
/// Ngày không tồn tại (31/02) → câu lỗi tiếng Việt cụ thể, trả về 400 qua InvalidModelStateResponseFactory.
/// Tự áp dụng cho cả DateTime? (System.Text.Json tự bọc Nullable).
/// </summary>
public sealed class StrictDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(DateParsing.FormatError);

        if (reader.TryGetDateTime(out var iso)) return iso;

        var s = reader.GetString();
        if (DateParsing.TryParse(s, out var date, out var hasTime, out var error))
        {
            // Phần ngày đúng nhưng phần giờ sai (vd "2026-02-01T25:00") → không được lặng lẽ bỏ giờ
            if (hasTime) throw new JsonException("Giờ không hợp lệ (định dạng HH:mm:ss)");
            return date.ToDateTime(TimeOnly.MinValue);
        }
        throw new JsonException(error);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
