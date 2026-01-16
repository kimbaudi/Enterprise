using System.ComponentModel.DataAnnotations;

namespace Enterprise.WebApi.Configuration;

/// <summary>
/// Rate limiting configuration with validation
/// </summary>
public class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Global rate limit configuration
    /// </summary>
    [Required]
    public RateLimitPolicy Global { get; set; } = new();

    /// <summary>
    /// Authentication endpoints rate limit configuration
    /// </summary>
    [Required]
    public RateLimitPolicy Auth { get; set; } = new();

    /// <summary>
    /// Expensive operations rate limit configuration
    /// </summary>
    [Required]
    public RateLimitPolicy Expensive { get; set; } = new();
}

/// <summary>
/// Individual rate limit policy configuration
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Maximum number of requests allowed in the time window
    /// </summary>
    [Range(1, 10000, ErrorMessage = "PermitLimit must be between 1 and 10000")]
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window duration (format: hh:mm:ss)
    /// </summary>
    [Required(ErrorMessage = "Window is required")]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
