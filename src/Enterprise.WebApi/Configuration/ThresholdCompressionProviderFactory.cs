using Microsoft.Extensions.Options;

namespace Enterprise.WebApi.Configuration;

/// <summary>
/// Custom compression provider factory that wraps existing providers with size threshold logic
/// </summary>
public class ThresholdCompressionProviderFactory
{
    private readonly IOptions<CompressionOptions> _compressionOptions;

    public ThresholdCompressionProviderFactory(IOptions<CompressionOptions> compressionOptions)
    {
        _compressionOptions = compressionOptions;
    }

    public bool ShouldCompress(HttpContext context)
    {
        // Check content length if available
        if (context.Response.ContentLength.HasValue)
        {
            return context.Response.ContentLength.Value >= _compressionOptions.Value.MinimumSizeBytes;
        }

        // If content length is not known, allow compression
        // The actual compression will be handled by the provider
        return true;
    }
}
