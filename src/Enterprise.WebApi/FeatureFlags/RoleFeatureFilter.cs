using Microsoft.FeatureManagement;

namespace Enterprise.WebApi.FeatureFlags;

/// <summary>
/// Custom feature filter that enables features based on user roles.
/// Allows targeting specific features to Admin, Manager, or User roles.
/// </summary>
[FilterAlias("Role")]
public class RoleFeatureFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoleFeatureFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(false);
        }

        // Get required roles from configuration
        var settings = context.Parameters.Get<RoleFilterSettings>();
        if (settings?.RequiredRoles == null || settings.RequiredRoles.Length == 0)
        {
            return Task.FromResult(false);
        }

        // Check if user has any of the required roles
        var hasRequiredRole = settings.RequiredRoles.Any(role =>
            httpContext.User.IsInRole(role));

        return Task.FromResult(hasRequiredRole);
    }

    private class RoleFilterSettings
    {
        public string[] RequiredRoles { get; set; } = Array.Empty<string>();
    }
}
