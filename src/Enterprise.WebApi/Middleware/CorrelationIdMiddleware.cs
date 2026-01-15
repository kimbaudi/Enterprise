using Serilog.Context;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Middleware that manages correlation IDs for distributed tracing.
/// Ensures every request has a unique correlation ID that persists across logs and microservices.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to HttpContext for easy access throughout the request pipeline
        context.Items[CorrelationIdHeader] = correlationId;

        // Push correlation ID to Serilog's LogContext for automatic inclusion in all logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get from request header (supports distributed tracing across services)
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            var id = correlationId.ToString();
            context.Response.Headers[CorrelationIdHeader] = id;
            return id;
        }

        // Generate new correlation ID if not provided
        var newCorrelationId = Guid.NewGuid().ToString("N");
        context.Response.Headers[CorrelationIdHeader] = newCorrelationId;
        return newCorrelationId;
    }
}

/// <summary>
/// Extension methods for easily retrieving correlation ID from HttpContext
/// </summary>
public static class CorrelationIdExtensions
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// Gets the correlation ID for the current request
    /// </summary>
    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdHeader, out var correlationId) &&
            correlationId is string id)
        {
            return id;
        }

        return string.Empty;
    }
}
