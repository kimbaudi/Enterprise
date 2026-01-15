using System.Diagnostics;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Middleware that adds performance timing headers to responses
/// </summary>
public class ResponseTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimingMiddleware> _logger;

    public ResponseTimingMiddleware(
        RequestDelegate next,
        ILogger<ResponseTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Track database query time (would be set by repository/EF interceptor)
        context.Items["DbQueryStartTime"] = stopwatch.ElapsedMilliseconds;

        await _next(context);

        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Only add headers if response hasn't started (to avoid InvalidOperationException)
        if (!context.Response.HasStarted)
        {
            // Add timing headers
            context.Response.Headers["X-Response-Time"] = $"{responseTime}ms";

            // Add cache status if available
            if (context.Items.ContainsKey("CacheStatus"))
            {
                context.Response.Headers["X-Cache-Status"] = context.Items["CacheStatus"]?.ToString() ?? "MISS";
            }

            // Add database query time if tracked
            if (context.Items.ContainsKey("DbQueryTime"))
            {
                var dbTime = context.Items["DbQueryTime"];
                context.Response.Headers["X-DB-Query-Time"] = $"{dbTime}ms";
            }
        }

        // Log slow responses
        if (responseTime > 500)
        {
            _logger.LogWarning(
                "Slow response detected | Path: {Path} Method: {Method} Duration: {Duration}ms StatusCode: {StatusCode}",
                context.Request.Path,
                context.Request.Method,
                responseTime,
                context.Response.StatusCode);
        }
    }
}
