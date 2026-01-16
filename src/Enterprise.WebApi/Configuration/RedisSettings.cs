using System.ComponentModel.DataAnnotations;

namespace Enterprise.WebApi.Configuration;

/// <summary>
/// Redis cache configuration with validation
/// </summary>
public class RedisSettings
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string (e.g., "localhost:6379" or "redis.example.com:6379,password=xxx")
    /// </summary>
    [Required(ErrorMessage = "Redis Configuration (connection string) is required")]
    public string Configuration { get; set; } = string.Empty;

    /// <summary>
    /// Instance name prefix for cache keys (helps avoid key collisions in shared Redis)
    /// </summary>
    [Required(ErrorMessage = "Redis InstanceName is required")]
    [MinLength(1, ErrorMessage = "Redis InstanceName must not be empty")]
    public string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// Connection timeout in milliseconds (default: 5000ms)
    /// </summary>
    [Range(1000, 30000, ErrorMessage = "ConnectTimeout must be between 1000 and 30000 milliseconds")]
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// Enable connection retry on failure
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;
}
