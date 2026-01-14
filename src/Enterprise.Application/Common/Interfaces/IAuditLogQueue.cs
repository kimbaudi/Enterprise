using Enterprise.Domain.Entities;

namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Interface for enqueueing audit log entries for background processing.
/// Uses a thread-safe queue to prevent audit logging from blocking request execution.
/// </summary>
public interface IAuditLogQueue
{
    /// <summary>
    /// Enqueue an audit log entry for background processing.
    /// Non-blocking operation that returns immediately.
    /// </summary>
    /// <param name="auditLog">The audit log entry to queue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully enqueued, false if queue is full</returns>
    ValueTask<bool> EnqueueAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue an audit log entry for processing.
    /// Blocks until an item is available or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The next audit log entry to process</returns>
    ValueTask<AuditLog> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current number of items in the queue.
    /// </summary>
    int QueuedCount { get; }
}
