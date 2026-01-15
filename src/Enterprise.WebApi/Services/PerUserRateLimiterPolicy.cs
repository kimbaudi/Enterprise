using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Enterprise.WebApi.Services;

/// <summary>
/// Custom rate limiter that provides per-user rate limiting based on user ID and roles
/// </summary>
public class PerUserRateLimiterPolicy
{
    public static RateLimiterOptions ConfigurePerUserRateLimiting(RateLimiterOptions options)
    {
        // Per-user rate limiting based on authentication
        options.AddPolicy("perUser", httpContext =>
        {
            var username = httpContext.User.Identity?.Name ?? "anonymous";
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

            // Check user role for different limits
            var isAdmin = httpContext.User.IsInRole("Admin");
            var isPremium = httpContext.User.IsInRole("Manager"); // Treat Manager as premium

            int permitLimit;
            TimeSpan window;

            if (isAdmin)
            {
                // Admins get higher limits
                permitLimit = 1000;
                window = TimeSpan.FromMinutes(1);
            }
            else if (isPremium)
            {
                // Premium users get moderate limits
                permitLimit = 300;
                window = TimeSpan.FromMinutes(1);
            }
            else
            {
                // Free tier users get basic limits
                permitLimit = 100;
                window = TimeSpan.FromMinutes(1);
            }

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: userId,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0
                });
        });

        // Expensive operations - very restrictive per-user limits
        options.AddPolicy("expensive-per-user", httpContext =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

            var isAdmin = httpContext.User.IsInRole("Admin");

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: userId,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = isAdmin ? 50 : 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                });
        });

        // Authentication endpoints - track by IP and username attempt
        options.AddPolicy("auth-per-ip", httpContext =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: ipAddress,
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    SegmentsPerWindow = 5,
                    QueueLimit = 0
                });
        });

        return options;
    }

    /// <summary>
    /// Custom rejection response that includes rate limit headers
    /// </summary>
    public static Task OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        // Add rate limit headers
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = context.HttpContext.Request.RouteValues["limit"]?.ToString() ?? "100";
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.HttpContext.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(retryAfter).ToUnixTimeSeconds().ToString();

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        return context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            message = "Too many requests. Please try again later.",
            retryAfter = retryAfter.TotalSeconds,
            resetAt = DateTimeOffset.UtcNow.Add(retryAfter).ToString("O")
        }, cancellationToken);
    }
}
