using Enterprise.WebApi.Common;
using System.Text.Json;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Middleware that injects Request ID into API responses
/// </summary>
public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID (set by CorrelationIdMiddleware)
        var requestId = context.GetCorrelationId();

        // Add to response header
        context.Response.Headers["X-Request-ID"] = requestId;

        // Capture response body to inject requestId
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Only modify JSON responses
            if (context.Response.ContentType?.Contains("application/json") == true)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(responseBody).ReadToEndAsync();

                // Try to parse and inject requestId
                if (!string.IsNullOrWhiteSpace(body) && body.StartsWith("{"))
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(body);

                        // Check if it's already an ApiResponse structure
                        if (jsonDoc.RootElement.TryGetProperty("success", out _) ||
                            jsonDoc.RootElement.TryGetProperty("Success", out _))
                        {
                            // It's an ApiResponse, inject requestId
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            };

                            using var stream = new MemoryStream();
                            using var writer = new Utf8JsonWriter(stream);

                            writer.WriteStartObject();

                            foreach (var property in jsonDoc.RootElement.EnumerateObject())
                            {
                                property.WriteTo(writer);
                            }

                            // Add requestId if not present
                            if (!jsonDoc.RootElement.TryGetProperty("requestId", out _) &&
                                !jsonDoc.RootElement.TryGetProperty("RequestId", out _))
                            {
                                writer.WriteString("requestId", requestId);
                            }

                            writer.WriteEndObject();
                            await writer.FlushAsync();

                            body = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                    catch
                    {
                        // If parsing fails, leave body as-is
                    }
                }

                responseBody.SetLength(0);
                await responseBody.WriteAsync(System.Text.Encoding.UTF8.GetBytes(body));
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
