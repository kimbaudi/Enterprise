namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Service for tracking application metrics and counters.
/// Provides instrumentation for observability and monitoring.
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Increment total request count
    /// </summary>
    void IncrementRequestCount(string method, string endpoint, int statusCode);

    /// <summary>
    /// Record cache hit
    /// </summary>
    void RecordCacheHit(string cacheType);

    /// <summary>
    /// Record cache miss
    /// </summary>
    void RecordCacheMiss(string cacheType);

    /// <summary>
    /// Record authentication failure
    /// </summary>
    void RecordAuthenticationFailure(string reason);

    /// <summary>
    /// Record authentication success
    /// </summary>
    void RecordAuthenticationSuccess(string method);

    /// <summary>
    /// Update audit log queue depth
    /// </summary>
    void UpdateAuditLogQueueDepth(int depth);

    /// <summary>
    /// Record command execution
    /// </summary>
    void RecordCommandExecution(string commandName, bool success, long durationMs);

    /// <summary>
    /// Record query execution
    /// </summary>
    void RecordQueryExecution(string queryName, long durationMs);

    /// <summary>
    /// Record validation failure
    /// </summary>
    void RecordValidationFailure(string requestName, int errorCount);

    /// <summary>
    /// Record database operation
    /// </summary>
    void RecordDatabaseOperation(string operation, long durationMs, bool success);

    /// <summary>
    /// Increment active requests gauge
    /// </summary>
    void IncrementActiveRequests();

    /// <summary>
    /// Decrement active requests gauge
    /// </summary>
    void DecrementActiveRequests();

    /// <summary>
    /// Record Hangfire job execution
    /// </summary>
    void RecordJobExecution(string jobName, bool success, long durationMs);

    /// <summary>
    /// Record Hangfire job failure
    /// </summary>
    void RecordJobFailure(string jobName, string failureReason);

    /// <summary>
    /// Increment active Hangfire jobs gauge
    /// </summary>
    void IncrementActiveJobs();

    /// <summary>
    /// Decrement active Hangfire jobs gauge
    /// </summary>
    void DecrementActiveJobs();
}
