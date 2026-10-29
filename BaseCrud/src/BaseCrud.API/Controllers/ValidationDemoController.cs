using BaseCrud.API.Models;
using BaseCrud.Common.Responses;
using BaseCrud.Common.Validation;
using Microsoft.AspNetCore.Mvc;

namespace BaseCrud.API.Controllers;

/// <summary>
/// Thử luật validate trên Swagger, không ghi DB. Xóa controller này khi lên production.
/// Với DTO không đi qua BaseBL, gọi RuleValidator.ValidateOrThrow(dto) như bên dưới.
/// </summary>
[ApiController]
[Route("api/validation-demo")]
public class ValidationDemoController : ControllerBase
{
    [HttpPost]
    public ActionResult<ApiResponse<object>> Validate([FromBody] EmployeeForm form)
    {
        RuleValidator.ValidateOrThrow(form);
        return Ok(ApiResponse<object>.Ok(null, "Dữ liệu hợp lệ."));
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30 * 1024 * 1024)] // chặn cứng ở tầng server, trước cả luật FileSize
    public ActionResult<ApiResponse<object>> Upload([FromForm] UploadForm form)
    {
        RuleValidator.ValidateOrThrow(form);
        return Ok(ApiResponse<object>.Ok(null, "Tệp hợp lệ."));
    }
}
