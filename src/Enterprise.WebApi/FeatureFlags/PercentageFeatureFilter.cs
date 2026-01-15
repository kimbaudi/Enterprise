using Microsoft.FeatureManagement;

namespace Enterprise.WebApi.FeatureFlags;

/// <summary>
/// Custom feature filter that enables features for a percentage of users.
/// Useful for gradual rollouts and A/B testing.
/// </summary>
[FilterAlias("Percentage")]
public class PercentageFeatureFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PercentageFeatureFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var settings = context.Parameters.Get<PercentageFilterSettings>();
        if (settings == null)
        {
            return Task.FromResult(false);
        }

        // Get user identifier from claims or IP address
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst("sub")?.Value
                     ?? httpContext?.User?.FindFirst("userId")?.Value
                     ?? httpContext?.Connection?.RemoteIpAddress?.ToString()
                     ?? Guid.NewGuid().ToString();

        // Calculate hash percentage (0-100)
        var hashCode = Math.Abs(userId.GetHashCode());
        var percentage = hashCode % 100;

        // Enable if percentage is within rollout range
        var isEnabled = percentage < settings.RolloutPercentage;

        return Task.FromResult(isEnabled);
    }

    private class PercentageFilterSettings
    {
        public int RolloutPercentage { get; set; }
    }
}
