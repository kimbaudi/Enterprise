using System.ComponentModel.DataAnnotations;

namespace Enterprise.WebApi.Configuration;

/// <summary>
/// Audit log processing configuration with validation
/// </summary>
public class AuditLogSettings
{
    public const string SectionName = "AuditLog";

    /// <summary>
    /// Maximum queue capacity for audit logs (default: 10000)
    /// </summary>
    [Range(100, 100000, ErrorMessage = "QueueCapacity must be between 100 and 100000")]
    public int QueueCapacity { get; set; } = 10000;

    /// <summary>
    /// Number of audit logs to process in each batch (default: 50)
    /// </summary>
    [Range(1, 1000, ErrorMessage = "BatchSize must be between 1 and 1000")]
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Delay in milliseconds before processing a batch (default: 1000ms)
    /// </summary>
    [Range(100, 10000, ErrorMessage = "BatchDelayMs must be between 100 and 10000")]
    public int BatchDelayMs { get; set; } = 1000;

    /// <summary>
    /// Processing interval in milliseconds (default: 100ms)
    /// </summary>
    [Range(10, 5000, ErrorMessage = "ProcessingIntervalMs must be between 10 and 5000")]
    public int ProcessingIntervalMs { get; set; } = 100;
}
