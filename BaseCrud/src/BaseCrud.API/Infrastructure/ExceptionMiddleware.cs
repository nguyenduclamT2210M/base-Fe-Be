using BaseCrud.Common.Exceptions;
using BaseCrud.Common.Helpers;
using BaseCrud.Common.Responses;
using Microsoft.Data.SqlClient;

namespace BaseCrud.API.Infrastructure;

/// <summary>Bắt mọi exception → HTTP status + ApiResponse thống nhất. Không lộ stack trace ra client.</summary>
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, body) = Map(ex);

            if (status >= 500) logger.LogError(ex, "Lỗi không xử lý được: {Path}", context.Request.Path);
            else logger.LogInformation("{Status} {Path}: {Message}", status, context.Request.Path, ex.Message);

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(body, JsonDefaults.Options);
        }
    }

    private static (int Status, ApiResponse<object> Body) Map(Exception ex) => ex switch
    {
        ValidateException v => (StatusCodes.Status400BadRequest, ApiResponse<object>.Fail(v.Message, v.Errors)),
        NotFoundException => (StatusCodes.Status404NotFound, ApiResponse<object>.Fail(ex.Message)),
        ConcurrencyException => (StatusCodes.Status409Conflict, ApiResponse<object>.Fail(ex.Message)),

        // 2601/2627: vi phạm unique index/constraint (lớp chặn cuối khi 2 request insert trùng cùng lúc)
        SqlException { Number: 2601 or 2627 } =>
            (StatusCodes.Status409Conflict, ApiResponse<object>.Fail("Dữ liệu bị trùng với bản ghi đã có.")),

        // 547: vi phạm khóa ngoại (xóa bản ghi đang được tham chiếu, hoặc tham chiếu tới bản ghi không tồn tại)
        SqlException { Number: 547 } =>
            (StatusCodes.Status409Conflict, ApiResponse<object>.Fail("Vi phạm ràng buộc khóa ngoại.")),

        OperationCanceledException =>
            (499, ApiResponse<object>.Fail("Request đã bị hủy.")),

        _ => (StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail("Lỗi hệ thống. Vui lòng thử lại sau.")),
    };
}
