using System.ComponentModel.DataAnnotations;

namespace Enterprise.WebApi.Configuration;

/// <summary>
/// Configuration options for response compression
/// </summary>
public class CompressionOptions
{
    /// <summary>
    /// Minimum response size in bytes to trigger compression (default: 1KB)
    /// Responses smaller than this will not be compressed to save CPU overhead
    /// </summary>
    [Range(0, 1048576, ErrorMessage = "MinimumSizeBytes must be between 0 and 1MB (1048576 bytes)")]
    public int MinimumSizeBytes { get; set; } = 1024;

    /// <summary>
    /// Whether to enable compression for HTTPS responses
    /// Should be true in production with TLS 1.3+
    /// </summary>
    public bool EnableForHttps { get; set; } = true;

    /// <summary>
    /// Comma-separated list of additional MIME types to compress
    /// </summary>
    public string[] AdditionalMimeTypes { get; set; } = Array.Empty<string>();
}
