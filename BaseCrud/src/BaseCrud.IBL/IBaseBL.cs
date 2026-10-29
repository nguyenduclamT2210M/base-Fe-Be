using BaseCrud.Common.Entities;
using BaseCrud.Common.Query;
using BaseCrud.Common.Validation;

namespace BaseCrud.IBL;

public interface IBaseBL<T> where T : BaseEntity
{
    /// <summary>Không tìm thấy → NotFoundException.</summary>
    Task<T> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<T>> GetPagedAsync(QueryRequest request, CancellationToken ct = default);

    Task<T> InsertAsync(T entity, CancellationToken ct = default);

    /// <summary>Cập nhật toàn bộ (PUT). Entity có RowVersion thì bắt buộc gửi RowVersion.</summary>
    Task<T> UpdateAsync(Guid id, T entity, CancellationToken ct = default);

    Task DeleteAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra giá trị đã tồn tại chưa — cho frontend gọi trước khi submit.
    /// Chỉ chạy với trường có [Unique]; excludeId = bản ghi đang sửa.
    /// </summary>
    Task<UniqueCheckResult> CheckUniqueAsync(string field, string? value, Guid? excludeId, CancellationToken ct = default);
}
