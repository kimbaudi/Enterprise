using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Middleware that logs all HTTP requests and responses with sanitization of sensitive data.
/// Provides full observability of API traffic for debugging and auditing purposes.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private static readonly string[] SensitiveHeaders =
    {
        "Authorization", "Cookie", "X-API-Key", "X-Auth-Token", "X-Api-Key",
        "Api-Key", "ApiKey", "Authentication"
    };
    private static readonly string[] SensitiveBodyFields =
    {
        "password", "Password", "currentPassword", "newPassword",
        "token", "Token", "secret", "Secret", "apiKey", "ApiKey"
    };
    private const int MaxRequestBodyLogSize = 8192; // 8KB limit
    private const int MaxResponseBodyLogSize = 8192; // 8KB limit

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();

        // Log request
        await LogRequestAsync(context, correlationId);

        // Capture original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Execute the rest of the pipeline
            await _next(context);

            stopwatch.Stop();

            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds, responseBody);

            // Copy response body back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        try
        {
            var request = context.Request;

            // Read request body if present
            string? requestBody = null;
            if (request.ContentLength > 0 && request.ContentLength <= MaxRequestBodyLogSize)
            {
                request.EnableBuffering();
                request.Body.Position = 0;

                using var reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                // Sanitize sensitive data
                requestBody = SanitizeRequestBody(requestBody, request.ContentType);
            }

            // Sanitize headers
            var sanitizedHeaders = request.Headers
                .Where(h => !SensitiveHeaders.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            _logger.LogInformation(
                "HTTP Request: {Method} {Path}{QueryString} | CorrelationId: {CorrelationId} | ContentType: {ContentType} | ContentLength: {ContentLength} | UserAgent: {UserAgent} | ClientIP: {ClientIP}",
                request.Method,
                request.Path,
                request.QueryString,
                correlationId,
                request.ContentType ?? "N/A",
                request.ContentLength ?? 0,
                request.Headers["User-Agent"].ToString() ?? "N/A",
                context.Connection.RemoteIpAddress?.ToString() ?? "N/A");

            // Log request body if available (structured for better querying)
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogDebug(
                    "HTTP Request Body: {Method} {Path} | CorrelationId: {CorrelationId} | Body: {RequestBody}",
                    request.Method,
                    request.Path,
                    correlationId,
                    requestBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log HTTP request. CorrelationId: {CorrelationId}", correlationId);
        }
    }

    private async Task LogResponseAsync(
        HttpContext context,
        string correlationId,
        long elapsedMilliseconds,
        MemoryStream responseBody)
    {
        try
        {
            var response = context.Response;

            // Read response body if within size limit
            string? responseBodyText = null;
            if (responseBody.Length > 0 && responseBody.Length <= MaxResponseBodyLogSize)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
                responseBodyText = await reader.ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                // Sanitize sensitive data in response
                responseBodyText = SanitizeResponseBody(responseBodyText, response.ContentType);
            }

            var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                          response.StatusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;

            _logger.Log(
                logLevel,
                "HTTP Response: {Method} {Path} | StatusCode: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId} | ContentType: {ContentType} | ContentLength: {ContentLength}",
                context.Request.Method,
                context.Request.Path,
                response.StatusCode,
                elapsedMilliseconds,
                correlationId,
                response.ContentType ?? "N/A",
                responseBody.Length);

            // Log response body at debug level if available
            if (!string.IsNullOrWhiteSpace(responseBodyText))
            {
                _logger.LogDebug(
                    "HTTP Response Body: {Method} {Path} | StatusCode: {StatusCode} | CorrelationId: {CorrelationId} | Body: {ResponseBody}",
                    context.Request.Method,
                    context.Request.Path,
                    response.StatusCode,
                    correlationId,
                    responseBodyText.Length > 1000 ? $"{responseBodyText[..1000]}..." : responseBodyText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log HTTP response. CorrelationId: {CorrelationId}", correlationId);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        const string correlationIdHeader = "X-Correlation-ID";

        // Try to get from request header
        if (context.Request.Headers.TryGetValue(correlationIdHeader, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            // Set on response so client can correlate
            context.Response.Headers[correlationIdHeader] = correlationId.ToString();
            return correlationId.ToString();
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Response.Headers[correlationIdHeader] = newCorrelationId;
        return newCorrelationId;
    }

    private string SanitizeRequestBody(string body, string? contentType)
    {
        // Only sanitize JSON bodies
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return "[Non-JSON content]";
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(body);
            var sanitized = SanitizeJsonElement(jsonDoc.RootElement);
            return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return "[Unable to parse JSON]";
        }
    }

    private string SanitizeResponseBody(string body, string? contentType)
    {
        // Only sanitize JSON responses
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return "[Non-JSON content]";
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(body);
            var sanitized = SanitizeJsonElement(jsonDoc.RootElement);
            return JsonSerializer.Serialize(sanitized, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return body; // Return as-is if not JSON
        }
    }

    private object SanitizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    // Check if property name contains sensitive keywords
                    if (SensitiveBodyFields.Any(s => property.Name.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        obj[property.Name] = "***REDACTED***";
                    }
                    else
                    {
                        obj[property.Name] = SanitizeJsonElement(property.Value);
                    }
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(SanitizeJsonElement).ToList();

            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;

            case JsonValueKind.Number:
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return "null";

            default:
                return element.GetRawText();
        }
    }
}
