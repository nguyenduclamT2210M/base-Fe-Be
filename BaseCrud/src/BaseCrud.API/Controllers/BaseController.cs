using BaseCrud.Common.Entities;
using BaseCrud.Common.Query;
using BaseCrud.Common.Responses;
using BaseCrud.Common.Validation;
using BaseCrud.IBL;
using Microsoft.AspNetCore.Mvc;

namespace BaseCrud.API.Controllers;

/// <summary>
/// 5 endpoint CRUD chung. Controller con chỉ cần [Route] + constructor.
/// Controller KHÔNG chứa nghiệp vụ: nhận request → gọi BL → bọc ApiResponse.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseController<T> : ControllerBase where T : BaseEntity, new()
{
    protected readonly IBaseBL<T> BL;

    protected BaseController(IBaseBL<T> bl) => BL = bl;

    /// <summary>GET api/xxx/{id}</summary>
    [HttpGet("{id:guid}")]
    public virtual async Task<ActionResult<ApiResponse<T>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<T>.Ok(await BL.GetByIdAsync(id, ct)));

    /// <summary>
    /// POST api/xxx/query — dùng POST vì filter lồng nhau khó biểu diễn trên query string.
    /// </summary>
    [HttpPost("query")]
    public virtual async Task<ActionResult<ApiResponse<PagedResult<T>>>> Query(
        [FromBody] QueryRequest request, CancellationToken ct)
        => Ok(ApiResponse<PagedResult<T>>.Ok(await BL.GetPagedAsync(request, ct)));

    /// <summary>POST api/xxx</summary>
    [HttpPost]
    public virtual async Task<ActionResult<ApiResponse<T>>> Insert([FromBody] T entity, CancellationToken ct)
    {
        var created = await BL.InsertAsync(entity, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<T>.Ok(created));
    }

    /// <summary>PUT api/xxx/{id} — body phải chứa rowVersion nhận được từ lần GET.</summary>
    [HttpPut("{id:guid}")]
    public virtual async Task<ActionResult<ApiResponse<T>>> Update(Guid id, [FromBody] T entity, CancellationToken ct)
        => Ok(ApiResponse<T>.Ok(await BL.UpdateAsync(id, entity, ct)));

    /// <summary>
    /// GET api/xxx/check-unique?field=code&amp;value=NV001&amp;excludeId={id đang sửa}
    /// Frontend gọi khi rời ô nhập (blur). Chỉ nhận trường có [Unique].
    /// </summary>
    [HttpGet("check-unique")]
    public virtual async Task<ActionResult<ApiResponse<UniqueCheckResult>>> CheckUnique(
        [FromQuery] string field, [FromQuery] string? value, [FromQuery] Guid? excludeId, CancellationToken ct)
        => Ok(ApiResponse<UniqueCheckResult>.Ok(await BL.CheckUniqueAsync(field, value, excludeId, ct)));

    /// <summary>
    /// DELETE api/xxx/{id}?rowVersion=base64 — rowVersion tùy chọn; nhớ URL-encode (base64 có + / =).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public virtual async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id, [FromQuery] byte[]? rowVersion, CancellationToken ct)
    {
        await BL.DeleteAsync(id, rowVersion, ct);
        return Ok(ApiResponse<object>.Ok(null, "Đã xóa."));
    }
}
