using BaseCrud.Common;

namespace BaseCrud.API.Infrastructure;

/// <summary>Lấy user từ HttpContext. Khi chưa có auth sẽ trả "anonymous".</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserName
    {
        get
        {
            var identity = accessor.HttpContext?.User.Identity;
            return identity?.IsAuthenticated == true ? identity.Name : "anonymous";
        }
    }
}
