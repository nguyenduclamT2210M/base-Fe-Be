namespace BaseCrud.Common.Exceptions;

/// <summary>Dữ liệu đầu vào sai → HTTP 400.</summary>
public class ValidateException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidateException(IDictionary<string, string[]> errors, string message = "Dữ liệu không hợp lệ.")
        : base(message) => Errors = errors;

    public ValidateException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = new[] { error } }, error) { }
}

/// <summary>Không tìm thấy bản ghi → HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id)
        : base($"Không tìm thấy {entityName} với Id = {id}.") { }

    public NotFoundException(string message) : base(message) { }
}

/// <summary>Bản ghi đã bị người khác sửa/xóa sau lần đọc gần nhất → HTTP 409.</summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string entityName, Guid id)
        : base($"{entityName} (Id = {id}) đã bị thay đổi bởi người khác. Hãy tải lại dữ liệu rồi thử lại.") { }
}
