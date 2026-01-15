using Enterprise.WebApi.Configuration;
using Microsoft.Extensions.Options;

namespace Enterprise.WebApi.Middleware;

/// <summary>
/// Middleware that prevents compression of responses below a certain size threshold.
/// This saves CPU overhead for small responses where compression provides minimal benefit.
/// </summary>
public class CompressionThresholdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CompressionOptions _options;

    public CompressionThresholdMiddleware(
        RequestDelegate next,
        IOptions<CompressionOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);

            // Check if response size is below threshold
            if (memoryStream.Length < _options.MinimumSizeBytes)
            {
                // Remove compression-related headers for small responses
                context.Response.Headers.Remove("Content-Encoding");
                context.Response.Headers.Remove("Vary");
            }

            // Copy response to original stream
            context.Response.ContentLength = memoryStream.Length;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
