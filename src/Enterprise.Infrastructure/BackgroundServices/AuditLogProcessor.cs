using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Enterprise.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes queued audit log entries.
/// Processes entries in batches for better database performance.
/// </summary>
public class AuditLogProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuditLogQueue _auditLogQueue;
    private readonly ILogger<AuditLogProcessor> _logger;
    private const int BatchSize = 50; // Process 50 audit logs at a time
    private const int BatchDelayMs = 1000; // Wait 1 second before processing batch

    public AuditLogProcessor(
        IServiceProvider serviceProvider,
        IAuditLogQueue auditLogQueue,
        ILogger<AuditLogProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _auditLogQueue = auditLogQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Log Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audit log batch");
                await Task.Delay(5000, stoppingToken); // Wait 5 seconds before retrying
            }
        }

        _logger.LogInformation("Audit Log Processor stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var batch = new List<Domain.Entities.AuditLog>();
        var batchTimer = DateTime.UtcNow;

        // Collect batch of audit logs
        while (batch.Count < BatchSize)
        {
            // Check if we should process the batch (either full or time elapsed)
            if (batch.Count > 0 && (DateTime.UtcNow - batchTimer).TotalMilliseconds >= BatchDelayMs)
            {
                break;
            }

            try
            {
                // Try to dequeue with a short timeout to allow batch processing
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(100));

                var auditLog = await _auditLogQueue.DequeueAsync(cts.Token);
                batch.Add(auditLog);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout waiting for next item, process what we have
                if (batch.Count > 0)
                {
                    break;
                }
                // No items in batch, continue waiting
                await Task.Delay(100, cancellationToken);
            }
        }

        // Save batch to database
        if (batch.Count > 0)
        {
            await SaveBatchAsync(batch, cancellationToken);
        }
    }

    private async Task SaveBatchAsync(List<Domain.Entities.AuditLog> batch, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditLogRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // Add all audit logs in batch
            foreach (var auditLog in batch)
            {
                await auditLogRepository.AddAsync(auditLog, cancellationToken);
            }

            // Save all changes in one transaction
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processed batch of {Count} audit logs", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save batch of {Count} audit logs", batch.Count);

            // Log details of failed entries for debugging
            foreach (var auditLog in batch)
            {
                _logger.LogWarning(
                    "Failed audit log: User {Username} performed {Action} on {EntityName} (ID: {EntityId})",
                    auditLog.Username, auditLog.Action, auditLog.EntityName, auditLog.EntityId);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit Log Processor is stopping. Processing remaining items...");

        // Process any remaining items in the queue before shutdown
        while (_auditLogQueue.QueuedCount > 0 && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing remaining audit logs during shutdown");
                break;
            }
        }

        await base.StopAsync(cancellationToken);
    }
}
