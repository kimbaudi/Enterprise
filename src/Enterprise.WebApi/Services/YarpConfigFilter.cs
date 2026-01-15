using Yarp.ReverseProxy.Configuration;

namespace Enterprise.WebApi.Services;

/// <summary>
/// YARP configuration filter for customizing reverse proxy behavior
/// </summary>
public class YarpConfigFilter : IProxyConfigFilter
{
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        // Add custom cluster configuration here if needed
        // For example: circuit breaker, timeout policies, etc.

        return new ValueTask<ClusterConfig>(cluster);
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        // Add custom route configuration here if needed
        // For example: authentication requirements, rate limiting, etc.

        return new ValueTask<RouteConfig>(route);
    }
}
