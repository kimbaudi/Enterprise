using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Enterprise.Application.Services;

/// <summary>
/// Thread-safe, high-performance queue for audit log entries using System.Threading.Channels.
/// Provides bounded capacity to prevent memory issues during high load.
/// </summary>
public class AuditLogQueue : IAuditLogQueue
{
    private readonly Channel<AuditLog> _queue;
    private readonly ILogger<AuditLogQueue> _logger;

    public AuditLogQueue(ILogger<AuditLogQueue> logger)
    {
        _logger = logger;

        // Create bounded channel with capacity of 10,000 items
        // Drop newest items if full (prevents blocking producers)
        var options = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.DropNewest,
            SingleWriter = false,
            SingleReader = true
        };

        _queue = Channel.CreateBounded<AuditLog>(options);
    }

    /// <summary>
    /// Enqueue an audit log entry. Non-blocking operation.
    /// </summary>
    public async ValueTask<bool> EnqueueAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        if (auditLog == null)
        {
            _logger.LogWarning("Attempted to enqueue null audit log");
            return false;
        }

        try
        {
            // TryWrite is synchronous and returns immediately
            if (_queue.Writer.TryWrite(auditLog))
            {
                return true;
            }

            // If TryWrite fails, try async write (will drop newest if full)
            await _queue.Writer.WriteAsync(auditLog, cancellationToken);
            return true;
        }
        catch (ChannelClosedException)
        {
            _logger.LogError("Cannot enqueue audit log - channel is closed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue audit log for entity {EntityName} (ID: {EntityId})",
                auditLog.EntityName, auditLog.EntityId);
            return false;
        }
    }

    /// <summary>
    /// Dequeue an audit log entry. Blocks until an item is available.
    /// </summary>
    public async ValueTask<AuditLog> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning("Audit log queue channel closed during read");
            throw;
        }
    }

    /// <summary>
    /// Get the current number of queued items.
    /// </summary>
    public int QueuedCount => _queue.Reader.Count;
}
