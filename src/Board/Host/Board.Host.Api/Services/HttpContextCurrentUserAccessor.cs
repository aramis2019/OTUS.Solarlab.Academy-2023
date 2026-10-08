using System.Security.Claims;
using Board.Application.AppData.Common;

namespace Board.Host.Api.Services;

/// <summary>
/// Получает текущего пользователя из claims HTTP-запроса.
/// </summary>
public class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Инициализирует экземпляр <see cref="HttpContextCurrentUserAccessor"/>.
    /// </summary>
    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Guid? GetCurrentAccountId()
    {
        var claimId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claimId, out var id) ? id : null;
    }

    /// <inheritdoc />
    public bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) ?? false;
    }
}
