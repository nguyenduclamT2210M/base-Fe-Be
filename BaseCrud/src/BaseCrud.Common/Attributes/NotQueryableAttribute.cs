namespace BaseCrud.Common.Attributes;

/// <summary>
/// Chặn client filter / sort / chọn cột theo property này qua API query động
/// (ví dụ PasswordHash, ghi chú nội bộ). Cột vẫn được đọc/ghi bình thường.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotQueryableAttribute : Attribute;
