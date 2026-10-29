using BaseCrud.Common.Entities;
using BaseCrud.Common.Query;

namespace BaseCrud.IDL;

public interface IBaseDL<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Filter / sort / paging / chọn cột động.</summary>
    Task<PagedResult<T>> GetPagedAsync(QueryRequest request, CancellationToken ct = default);

    /// <summary>Trả về bản ghi vừa thêm (đọc lại từ DB qua OUTPUT INSERTED, có RowVersion mới).</summary>
    Task<T> InsertAsync(T entity, CancellationToken ct = default);

    /// <summary>Trả về null nếu không có dòng nào bị ảnh hưởng (không tồn tại hoặc sai RowVersion).</summary>
    Task<T?> UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>Xóa mềm nếu entity là ISoftDelete, ngược lại xóa cứng. Trả về số dòng bị ảnh hưởng.</summary>
    Task<int> DeleteAsync(Guid id, byte[]? rowVersion, string? actor, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Kiểm tra giá trị đã được dùng chưa (validate unique), bỏ qua bản ghi excludeId.</summary>
    Task<bool> IsValueTakenAsync(string propertyName, object? value, Guid? excludeId = null, CancellationToken ct = default);
}
