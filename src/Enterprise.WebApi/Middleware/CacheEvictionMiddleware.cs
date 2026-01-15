using Microsoft.AspNetCore.OutputCaching;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Middleware to evict output cache when mutation operations occur
/// </summary>
public class CacheEvictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CacheEvictionMiddleware> _logger;

    public CacheEvictionMiddleware(RequestDelegate next, ILogger<CacheEvictionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOutputCacheStore outputCache)
    {
        await _next(context);

        // Only evict cache on successful mutations (POST, PUT, DELETE)
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

            if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                // Evict product-related caches
                if (path.Contains("/products"))
                {
                    await outputCache.EvictByTagAsync("products", context.RequestAborted);
                    _logger.LogInformation("Evicted output cache for tag: products");
                }
                // Evict user-related caches
                else if (path.Contains("/users"))
                {
                    await outputCache.EvictByTagAsync("users", context.RequestAborted);
                    _logger.LogInformation("Evicted output cache for tag: users");
                }
            }
        }
    }
}
