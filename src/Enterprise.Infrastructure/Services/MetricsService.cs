using Enterprise.Application.Common.Interfaces;
using System.Diagnostics.Metrics;

namespace Enterprise.Infrastructure.Services;

/// <summary>
/// Implementation of metrics service using System.Diagnostics.Metrics API.
/// Provides OpenTelemetry-compatible metrics for monitoring and observability.
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    private readonly Counter<long> _authFailureCounter;
    private readonly Counter<long> _authSuccessCounter;
    private readonly Counter<long> _commandCounter;
    private readonly Counter<long> _queryCounter;
    private readonly Counter<long> _validationFailureCounter;
    private readonly Counter<long> _databaseOperationCounter;
    private readonly Histogram<long> _commandDuration;
    private readonly Histogram<long> _queryDuration;
    private readonly Histogram<long> _databaseOperationDuration;
    private readonly ObservableGauge<int> _auditLogQueueDepth;
    private readonly UpDownCounter<int> _activeRequests;
    private readonly Counter<long> _jobCounter;
    private readonly Counter<long> _jobFailureCounter;
    private readonly Histogram<long> _jobDuration;
    private readonly UpDownCounter<int> _activeJobs;

    private int _currentAuditLogQueueDepth;

    public MetricsService()
    {
        _meter = new Meter("Enterprise.WebApi", "1.0.0");

        // Counters
        _requestCounter = _meter.CreateCounter<long>(
            "enterprise.requests.total",
            unit: "requests",
            description: "Total number of HTTP requests");

        _cacheHitCounter = _meter.CreateCounter<long>(
            "enterprise.cache.hits",
            unit: "hits",
            description: "Number of cache hits");

        _cacheMissCounter = _meter.CreateCounter<long>(
            "enterprise.cache.misses",
            unit: "misses",
            description: "Number of cache misses");

        _authFailureCounter = _meter.CreateCounter<long>(
            "enterprise.auth.failures",
            unit: "failures",
            description: "Number of authentication failures");

        _authSuccessCounter = _meter.CreateCounter<long>(
            "enterprise.auth.successes",
            unit: "successes",
            description: "Number of successful authentications");

        _commandCounter = _meter.CreateCounter<long>(
            "enterprise.commands.total",
            unit: "commands",
            description: "Total number of CQRS commands executed");

        _queryCounter = _meter.CreateCounter<long>(
            "enterprise.queries.total",
            unit: "queries",
            description: "Total number of CQRS queries executed");

        _validationFailureCounter = _meter.CreateCounter<long>(
            "enterprise.validation.failures",
            unit: "failures",
            description: "Number of validation failures");

        _databaseOperationCounter = _meter.CreateCounter<long>(
            "enterprise.database.operations",
            unit: "operations",
            description: "Total number of database operations");

        // Histograms for duration tracking
        _commandDuration = _meter.CreateHistogram<long>(
            "enterprise.commands.duration",
            unit: "ms",
            description: "Duration of CQRS command execution");

        _queryDuration = _meter.CreateHistogram<long>(
            "enterprise.queries.duration",
            unit: "ms",
            description: "Duration of CQRS query execution");

        _databaseOperationDuration = _meter.CreateHistogram<long>(
            "enterprise.database.duration",
            unit: "ms",
            description: "Duration of database operations");

        // Gauges
        _auditLogQueueDepth = _meter.CreateObservableGauge(
            "enterprise.auditlog.queue.depth",
            () => _currentAuditLogQueueDepth,
            unit: "items",
            description: "Current depth of audit log queue");

        // UpDownCounter for active requests
        _activeRequests = _meter.CreateUpDownCounter<int>(
            "enterprise.requests.active",
            unit: "requests",
            description: "Number of active HTTP requests");

        // Hangfire job metrics
        _jobCounter = _meter.CreateCounter<long>(
            "enterprise.jobs.total",
            unit: "jobs",
            description: "Total number of Hangfire jobs executed");

        _jobFailureCounter = _meter.CreateCounter<long>(
            "enterprise.jobs.failures",
            unit: "failures",
            description: "Number of Hangfire job failures");

        _jobDuration = _meter.CreateHistogram<long>(
            "enterprise.jobs.duration",
            unit: "ms",
            description: "Duration of Hangfire job execution");

        _activeJobs = _meter.CreateUpDownCounter<int>(
            "enterprise.jobs.active",
            unit: "jobs",
            description: "Number of active Hangfire jobs");
    }

    public void IncrementRequestCount(string method, string endpoint, int statusCode)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object?>("method", method),
                               new KeyValuePair<string, object?>("endpoint", endpoint),
                               new KeyValuePair<string, object?>("status_code", statusCode));
    }

    public void RecordCacheHit(string cacheType)
    {
        _cacheHitCounter.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));
    }

    public void RecordCacheMiss(string cacheType)
    {
        _cacheMissCounter.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));
    }

    public void RecordAuthenticationFailure(string reason)
    {
        _authFailureCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }

    public void RecordAuthenticationSuccess(string method)
    {
        _authSuccessCounter.Add(1, new KeyValuePair<string, object?>("method", method));
    }

    public void UpdateAuditLogQueueDepth(int depth)
    {
        _currentAuditLogQueueDepth = depth;
    }

    public void RecordCommandExecution(string commandName, bool success, long durationMs)
    {
        _commandCounter.Add(1, new KeyValuePair<string, object?>("command", commandName),
                               new KeyValuePair<string, object?>("success", success));
        _commandDuration.Record(durationMs, new KeyValuePair<string, object?>("command", commandName));
    }

    public void RecordQueryExecution(string queryName, long durationMs)
    {
        _queryCounter.Add(1, new KeyValuePair<string, object?>("query", queryName));
        _queryDuration.Record(durationMs, new KeyValuePair<string, object?>("query", queryName));
    }

    public void RecordValidationFailure(string requestName, int errorCount)
    {
        _validationFailureCounter.Add(1, new KeyValuePair<string, object?>("request", requestName),
                                         new KeyValuePair<string, object?>("error_count", errorCount));
    }

    public void RecordDatabaseOperation(string operation, long durationMs, bool success)
    {
        _databaseOperationCounter.Add(1, new KeyValuePair<string, object?>("operation", operation),
                                         new KeyValuePair<string, object?>("success", success));
        _databaseOperationDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
    }

    public void IncrementActiveRequests()
    {
        _activeRequests.Add(1);
    }

    public void DecrementActiveRequests()
    {
        _activeRequests.Add(-1);
    }

    public void RecordJobExecution(string jobName, bool success, long durationMs)
    {
        _jobCounter.Add(1, new KeyValuePair<string, object?>("job", jobName),
                           new KeyValuePair<string, object?>("success", success));
        _jobDuration.Record(durationMs, new KeyValuePair<string, object?>("job", jobName));
    }

    public void RecordJobFailure(string jobName, string failureReason)
    {
        _jobFailureCounter.Add(1, new KeyValuePair<string, object?>("job", jobName),
                                  new KeyValuePair<string, object?>("reason", failureReason));
    }

    public void IncrementActiveJobs()
    {
        _activeJobs.Add(1);
    }

    public void DecrementActiveJobs()
    {
        _activeJobs.Add(-1);
    }
}
