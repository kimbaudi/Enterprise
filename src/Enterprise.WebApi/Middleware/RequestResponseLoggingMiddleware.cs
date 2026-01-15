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
        var correlationId = context.GetCorrelationId(); // Get from CorrelationIdMiddleware
        var stopwatch = Stopwatch.StartNew();

        // Log request with structured data
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

            // Structured logging with key-value pairs for better querying
            _logger.LogInformation(
                "HTTP Request | Method: {Method} Path: {Path} QueryString: {QueryString} ContentType: {ContentType} ContentLength: {ContentLength} UserAgent: {UserAgent} ClientIP: {ClientIP} Scheme: {Scheme} Host: {Host}",
                request.Method,
                request.Path.Value,
                request.QueryString.Value,
                request.ContentType ?? "N/A",
                request.ContentLength ?? 0,
                request.Headers["User-Agent"].FirstOrDefault() ?? "N/A",
                context.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                request.Scheme,
                request.Host.Value);

            // Log sanitized headers (structured)
            _logger.LogDebug(
                "HTTP Request Headers | Method: {Method} Path: {Path} Headers: {@Headers}",
                request.Method,
                request.Path.Value,
                sanitizedHeaders);

            // Log request body if available (structured for better querying)
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogDebug(
                    "HTTP Request Body | Method: {Method} Path: {Path} Body: {RequestBody}",
                    request.Method,
                    request.Path.Value,
                    requestBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log HTTP request | ErrorType: RequestLoggingFailure");
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

            // Structured response logging with performance metrics
            _logger.Log(
                logLevel,
                "HTTP Response | Method: {Method} Path: {Path} StatusCode: {StatusCode} Duration: {Duration}ms ContentType: {ContentType} ContentLength: {ContentLength} Success: {Success}",
                context.Request.Method,
                context.Request.Path.Value,
                response.StatusCode,
                elapsedMilliseconds,
                response.ContentType ?? "N/A",
                responseBody.Length,
                response.StatusCode < 400);

            // Log performance warning for slow requests
            if (elapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow Request Detected | Method: {Method} Path: {Path} Duration: {Duration}ms StatusCode: {StatusCode}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    elapsedMilliseconds,
                    response.StatusCode);
            }

            // Log response body at debug level if available
            if (!string.IsNullOrWhiteSpace(responseBodyText))
            {
                _logger.LogDebug(
                    "HTTP Response Body | Method: {Method} Path: {Path} StatusCode: {StatusCode} BodyLength: {BodyLength} Body: {ResponseBody}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    response.StatusCode,
                    responseBodyText.Length,
                    responseBodyText.Length > 1000 ? $"{responseBodyText[..1000]}..." : responseBodyText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log HTTP response | ErrorType: ResponseLoggingFailure");
        }
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
