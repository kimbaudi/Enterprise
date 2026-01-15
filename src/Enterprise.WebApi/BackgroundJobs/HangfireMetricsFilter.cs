using Enterprise.Application.Common.Interfaces;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using System.Diagnostics;

namespace Enterprise.WebApi.BackgroundJobs;

/// <summary>
/// Hangfire filter that tracks job execution metrics.
/// Automatically records job start, completion, duration, and failures.
/// </summary>
public class HangfireMetricsFilter : IServerFilter, IElectStateFilter
{
    private readonly IMetricsService _metricsService;
    private readonly ILogger<HangfireMetricsFilter> _logger;
    private const string StartTimeKey = "MetricsStartTime";

    public HangfireMetricsFilter(IMetricsService metricsService, ILogger<HangfireMetricsFilter> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    // IServerFilter - Called when job starts and finishes execution
    public void OnPerforming(PerformingContext context)
    {
        var jobName = context.BackgroundJob.Job.Type.Name;
        context.Items[StartTimeKey] = Stopwatch.GetTimestamp();
        _metricsService.IncrementActiveJobs();
        _logger.LogInformation("Hangfire job started: {JobName} (ID: {JobId})", jobName, context.BackgroundJob.Id);
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobName = context.BackgroundJob.Job.Type.Name;
        _metricsService.DecrementActiveJobs();

        if (context.Items.TryGetValue(StartTimeKey, out var startTimeObj) && startTimeObj is long startTime)
        {
            var durationMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            var success = context.Exception == null;

            _metricsService.RecordJobExecution(jobName, success, (long)durationMs);

            if (success)
            {
                _logger.LogInformation("Hangfire job completed successfully: {JobName} (Duration: {Duration}ms)",
                    jobName, durationMs);
            }
            else
            {
                var failureReason = context.Exception?.GetType().Name ?? "Unknown";
                _metricsService.RecordJobFailure(jobName, failureReason);
                _logger.LogError(context.Exception,
                    "Hangfire job failed: {JobName} (Duration: {Duration}ms, Reason: {Reason})",
                    jobName, durationMs, failureReason);
            }
        }
    }

    // IElectStateFilter - Called before state change is committed
    public void OnStateElection(ElectStateContext context)
    {
        // Track failed jobs
        if (context.CandidateState is FailedState failedState)
        {
            var jobName = context.BackgroundJob.Job.Type.Name;
            var failureReason = failedState.Exception?.GetType().Name ?? "Unknown";
            _metricsService.RecordJobFailure(jobName, failureReason);

            _logger.LogWarning("Hangfire job entering failed state: {JobName} (Reason: {Reason})",
                jobName, failureReason);
        }
    }
}
