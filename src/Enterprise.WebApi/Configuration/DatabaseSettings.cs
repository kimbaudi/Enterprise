using System.ComponentModel.DataAnnotations;

namespace Enterprise.WebApi.Configuration;

/// <summary>
/// Database configuration with validation
/// </summary>
public class DatabaseSettings
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// Default SQL Server connection string
    /// </summary>
    [Required(ErrorMessage = "DefaultConnection connection string is required")]
    [MinLength(10, ErrorMessage = "Connection string appears to be invalid (too short)")]
    public string DefaultConnection { get; set; } = string.Empty;

    /// <summary>
    /// Maximum retry count for transient failures (default: 5)
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetryCount must be between 0 and 10")]
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Maximum retry delay in seconds (default: 30)
    /// </summary>
    [Range(1, 120, ErrorMessage = "MaxRetryDelaySeconds must be between 1 and 120")]
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds (default: 30)
    /// </summary>
    [Range(5, 300, ErrorMessage = "CommandTimeoutSeconds must be between 5 and 300")]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
