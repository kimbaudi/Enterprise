using Enterprise.Application.Common.Interfaces;
using System.Diagnostics;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Middleware that tracks HTTP request metrics for observability.
/// Records request counts, duration, status codes, and active requests.
/// </summary>
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsService _metricsService;

    public MetricsMiddleware(RequestDelegate next, IMetricsService metricsService)
    {
        _next = next;
        _metricsService = metricsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _metricsService.IncrementActiveRequests();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.DecrementActiveRequests();

            // Record request metrics
            var method = context.Request.Method;
            var endpoint = context.Request.Path.ToString();
            var statusCode = context.Response.StatusCode;

            _metricsService.IncrementRequestCount(method, endpoint, statusCode);
        }
    }
}
