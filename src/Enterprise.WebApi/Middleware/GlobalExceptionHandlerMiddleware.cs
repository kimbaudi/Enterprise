using Enterprise.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions
/// and returns a consistent RFC 7807 Problem Details error response format
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] SensitiveHeaders = { "Authorization", "Cookie", "X-API-Key", "X-Auth-Token" };
    private const int MaxRequestBodyLogSize = 4096; // 4KB limit for request body logging

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate or retrieve correlation ID for distributed tracing
        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Don't handle if response has already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "The response has already started, the error handler will not be executed. TraceId: {TraceId}, CorrelationId: {CorrelationId}",
                    context.TraceIdentifier,
                    correlationId);
                throw;
            }

            await HandleExceptionAsync(context, ex, correlationId, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context, 
        Exception exception, 
        string correlationId,
        long elapsedMilliseconds)
    {
        var traceId = context.TraceIdentifier;
        var (statusCode, title, errorCode, errors, isTransient) = GetErrorDetails(exception, traceId, correlationId, context);

        var problemDetails = new ProblemDetailsResponse
        {
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = title,
            Status = (int)statusCode,
            Detail = GetDetailMessage(exception, statusCode),
            Instance = context.Request.Path,
            TraceId = traceId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Method = context.Request.Method,
            ErrorCode = errorCode,
            Errors = errors,
            IsTransient = isTransient
        };

        // Include additional debug information in development mode only
        if (_environment.IsDevelopment())
        {
            problemDetails.StackTrace = exception.StackTrace;
            problemDetails.InnerException = exception.InnerException?.Message;
            problemDetails.ExceptionType = exception.GetType().FullName;
            problemDetails.RequestHeaders = GetSanitizedHeaders(context);
            problemDetails.QueryString = context.Request.QueryString.Value;
            problemDetails.UserAgent = context.Request.Headers.UserAgent.ToString();
            problemDetails.ClientIp = GetClientIpAddress(context);
            problemDetails.RequestSize = context.Request.ContentLength;
            problemDetails.ElapsedMilliseconds = elapsedMilliseconds;
            
            // Log request body for POST/PUT/PATCH in development
            if (context.Request.ContentLength.HasValue && 
                context.Request.ContentLength > 0 &&
                (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH"))
            {
                problemDetails.RequestBody = await GetRequestBodyAsync(context);
            }
        }

        // Add response headers
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Response.Headers["X-Error-Code"] = errorCode;
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
        
        // Add Retry-After header for transient errors
        if (isTransient)
        {
            context.Response.Headers["Retry-After"] = "5"; // Suggest retry after 5 seconds
        }
        
        context.Response.ContentType = DetermineContentType(context);
        context.Response.StatusCode = (int)statusCode;

        var responseBody = await SerializeResponse(context, problemDetails);
        await context.Response.WriteAsync(responseBody);
    }

    private (HttpStatusCode StatusCode, string Title, string ErrorCode, object? Errors, bool IsTransient) GetErrorDetails(
        Exception exception, 
        string traceId,
        string correlationId,
        HttpContext context)
    {
        return exception switch
        {
            ValidationException validationEx => LogAndReturn(
                HttpStatusCode.BadRequest,
                "Validation Error",
                "VALIDATION_FAILED",
                validationEx.Errors,
                false,
                () => _logger.LogWarning(validationEx,
                    "Validation failed. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, Errors: {@Errors}",
                    traceId, correlationId, context.Request.Path, context.Request.Method, validationEx.Errors)
            ),

            NotFoundException notFoundEx => LogAndReturn(
                HttpStatusCode.NotFound,
                "Resource Not Found",
                "RESOURCE_NOT_FOUND",
                null,
                false,
                () => _logger.LogInformation(
                    "Resource not found. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, Message: {Message}",
                    traceId, correlationId, context.Request.Path, context.Request.Method, notFoundEx.Message)
            ),

            UnauthorizedAccessException unauthorizedEx => LogAndReturn(
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "UNAUTHORIZED_ACCESS",
                null,
                false,
                () => _logger.LogWarning(unauthorizedEx,
                    "Unauthorized access attempt. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, User: {User}, IP: {IP}",
                    traceId, correlationId, context.Request.Path, context.Request.Method, 
                    context.User?.Identity?.Name ?? "Anonymous", GetClientIpAddress(context))
            ),

            ArgumentNullException argNullEx => LogAndReturn(
                HttpStatusCode.BadRequest,
                "Bad Request",
                "MISSING_REQUIRED_PARAMETER",
                new { parameterName = argNullEx.ParamName },
                false,
                () => _logger.LogWarning(
                    "Required parameter missing. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Parameter: {Parameter}",
                    traceId, correlationId, argNullEx.ParamName)
            ),

            ArgumentException argEx => LogAndReturn(
                HttpStatusCode.BadRequest,
                "Bad Request",
                "INVALID_ARGUMENT",
                null,
                false,
                () => _logger.LogWarning(argEx,
                    "Invalid argument. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Message: {Message}",
                    traceId, correlationId, argEx.Message)
            ),

            InvalidOperationException invalidOpEx => LogAndReturn(
                HttpStatusCode.Conflict,
                "Conflict",
                "INVALID_OPERATION",
                null,
                false,
                () => _logger.LogWarning(invalidOpEx,
                    "Invalid operation. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Message: {Message}",
                    traceId, correlationId, invalidOpEx.Message)
            ),

            KeyNotFoundException keyNotFoundEx => LogAndReturn(
                HttpStatusCode.NotFound,
                "Resource Not Found",
                "KEY_NOT_FOUND",
                null,
                false,
                () => _logger.LogInformation(
                    "Key not found. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Message: {Message}",
                    traceId, correlationId, keyNotFoundEx.Message)
            ),

            TaskCanceledException or OperationCanceledException => LogAndReturn(
                HttpStatusCode.BadRequest,
                "Request Cancelled",
                "REQUEST_CANCELLED",
                null,
                true,
                () => _logger.LogInformation(
                    "Request was cancelled. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}",
                    traceId, correlationId, context.Request.Path)
            ),

            TimeoutException timeoutEx => LogAndReturn(
                HttpStatusCode.RequestTimeout,
                "Request Timeout",
                "REQUEST_TIMEOUT",
                null,
                true,
                () => _logger.LogWarning(timeoutEx,
                    "Request timeout. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                    traceId, correlationId, context.Request.Path, context.Request.Method)
            ),

            HttpRequestException httpEx => LogAndReturn(
                HttpStatusCode.BadGateway,
                "Downstream Service Error",
                "DOWNSTREAM_SERVICE_ERROR",
                null,
                true,
                () => _logger.LogError(httpEx,
                    "Downstream service error. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Message: {Message}",
                    traceId, correlationId, httpEx.Message)
            ),

            DbUpdateConcurrencyException concurrencyEx => LogAndReturn(
                HttpStatusCode.Conflict,
                "Concurrency Conflict",
                "CONCURRENCY_CONFLICT",
                new { message = "The record was modified by another user. Please refresh and try again." },
                true,
                () => _logger.LogWarning(concurrencyEx,
                    "Database concurrency conflict. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}",
                    traceId, correlationId, context.Request.Path)
            ),

            DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("UNIQUE") == true ||
                                        dbEx.InnerException?.Message.Contains("duplicate") == true => LogAndReturn(
                HttpStatusCode.Conflict,
                "Duplicate Entry",
                "DUPLICATE_ENTRY",
                new { message = "A record with the same key already exists." },
                false,
                () => _logger.LogWarning(dbEx,
                    "Duplicate key violation. TraceId: {TraceId}, CorrelationId: {CorrelationId}",
                    traceId, correlationId)
            ),

            DbUpdateException dbEx => LogAndReturn(
                HttpStatusCode.BadRequest,
                "Database Error",
                "DATABASE_ERROR",
                null,
                false,
                () => _logger.LogError(dbEx,
                    "Database update error. TraceId: {TraceId}, CorrelationId: {CorrelationId}, Path: {Path}",
                    traceId, correlationId, context.Request.Path)
            ),

            _ => HandleUnexpectedException(exception, traceId, correlationId, context)
        };
    }

    private (HttpStatusCode, string, string, object?, bool) LogAndReturn(
        HttpStatusCode statusCode,
        string title,
        string errorCode,
        object? errors,
        bool isTransient,
        Action logAction)
    {
        logAction();
        return (statusCode, title, errorCode, errors, isTransient);
    }

    private (HttpStatusCode, string, string, object?, bool) HandleUnexpectedException(
        Exception exception, 
        string traceId,
        string correlationId,
        HttpContext context)
    {
        _logger.LogError(exception,
            "An unexpected error occurred. TraceId: {TraceId}, CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}, Path: {Path}, Method: {Method}, User: {User}",
            traceId,
            correlationId,
            exception.GetType().Name,
            context.Request.Path,
            context.Request.Method,
            context.User?.Identity?.Name ?? "Anonymous");

        return (
            HttpStatusCode.InternalServerError,
            "Internal Server Error",
            "INTERNAL_SERVER_ERROR",
            null,
            false
        );
    }

    private string GetDetailMessage(Exception exception, HttpStatusCode statusCode)
    {
        // In production, hide sensitive error details for internal server errors
        if (!_environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            return "An unexpected error occurred. Please try again later or contact support if the problem persists.";
        }

        return exception.Message;
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) 
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = newCorrelationId;
        return newCorrelationId;
    }

    private Dictionary<string, string> GetSanitizedHeaders(HttpContext context)
    {
        return context.Request.Headers
            .Where(h => !SensitiveHeaders.Contains(h.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private async Task<string?> GetRequestBodyAsync(HttpContext context)
    {
        try
        {
            // Only read if content length is within limit
            if (context.Request.ContentLength > MaxRequestBodyLogSize)
            {
                return $"[Request body too large: {context.Request.ContentLength} bytes]";
            }

            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Sanitize potential sensitive data in request body
            return SanitizeRequestBody(body, context.Request.ContentType);
        }
        catch
        {
            return "[Unable to read request body]";
        }
    }

    private string SanitizeRequestBody(string body, string? contentType)
    {
        // Only attempt to sanitize JSON bodies
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return body;
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(body);
            var sanitizedJson = SanitizeJsonElement(jsonDoc.RootElement);
            return JsonSerializer.Serialize(sanitizedJson, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return body; // Return original if parsing fails
        }
    }

    private object? SanitizeJsonElement(JsonElement element)
    {
        var sensitiveFields = new[] { "password", "token", "secret", "apiKey", "creditCard", "ssn" };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name;
                    var isSensitive = sensitiveFields.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase));
                    dict[key] = isSensitive ? "***REDACTED***" : SanitizeJsonElement(property.Value);
                }
                return dict;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(SanitizeJsonElement).ToList();

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                return element.TryGetInt64(out var longValue) ? longValue : element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.ToString();
        }
    }

    private string DetermineContentType(HttpContext context)
    {
        // Check Accept header to determine preferred response format
        var acceptHeader = context.Request.Headers.Accept.FirstOrDefault();
        
        if (!string.IsNullOrEmpty(acceptHeader))
        {
            if (acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return "application/problem+json";
            }
            if (acceptHeader.Contains("application/xml", StringComparison.OrdinalIgnoreCase))
            {
                return "application/problem+xml";
            }
        }

        // Default to JSON
        return "application/problem+json";
    }

    private async Task<string> SerializeResponse(HttpContext context, ProblemDetailsResponse problemDetails)
    {
        var contentType = context.Response.ContentType ?? "application/problem+json";

        // For now, only support JSON (XML can be added later if needed)
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = _environment.IsDevelopment()
        };

        return JsonSerializer.Serialize(problemDetails, options);
    }

    private class ProblemDetailsResponse
    {
        /// <summary>
        /// A URI reference that identifies the problem type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// A short, human-readable summary of the problem type
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The HTTP status code
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// A human-readable explanation specific to this occurrence of the problem
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// A URI reference that identifies the specific occurrence of the problem
        /// </summary>
        public string Instance { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier for tracing the request within the application
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier for correlating requests across distributed systems
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// When the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// HTTP method of the request that caused the error
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Application-specific error code for programmatic handling
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Additional error details (e.g., validation errors)
        /// </summary>
        public object? Errors { get; set; }

        /// <summary>
        /// Stack trace (development only)
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Inner exception message (development only)
        /// </summary>
        public string? InnerException { get; set; }

        /// <summary>
        /// Full exception type name (development only)
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Sanitized request headers (development only)
        /// </summary>
        public Dictionary<string, string>? RequestHeaders { get; set; }

        /// <summary>
        /// Query string parameters (development only)
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// User agent of the client (development only)
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Client IP address (development only)
        /// </summary>
        public string? ClientIp { get; set; }

        /// <summary>
        /// Request content length in bytes (development only)
        /// </summary>
        public long? RequestSize { get; set; }

        /// <summary>
        /// Request body content (development only, sanitized)
        /// </summary>
        public string? RequestBody { get; set; }

        /// <summary>
        /// Time taken to process the request before exception (development only)
        /// </summary>
        public long? ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Indicates if the error is transient and can be retried
        /// </summary>
        public bool IsTransient { get; set; }
    }
}
