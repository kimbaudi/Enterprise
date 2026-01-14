using Enterprise.Application.Common.Interfaces;
using System.Security.Claims;

namespace Enterprise.WebApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User
        ?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Username => _httpContextAccessor.HttpContext?.User
        ?.FindFirstValue(ClaimTypes.Name);

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection
        .RemoteIpAddress?.ToString();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User
        ?.Identity?.IsAuthenticated ?? false;
}
