namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Provides resilience capabilities for various operations without exposing implementation details.
/// Implements circuit breaker, retry, and timeout patterns for external dependencies.
/// </summary>
public interface IResiliencePolicyProvider
{
    /// <summary>
    /// Executes a database operation with retry and circuit breaker protection.
    /// Includes 3 retries with exponential backoff and circuit breaker (50% failure threshold).
    /// </summary>
    Task<T> ExecuteDatabaseOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a cache operation with retry and circuit breaker protection.
    /// Includes 2 retries with exponential backoff and circuit breaker (50% failure threshold).
    /// Returns default value on failure to enable graceful degradation.
    /// </summary>
    Task<T?> ExecuteCacheOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes an external API call with retry, circuit breaker, and timeout (30s).
    /// Suitable for SendGrid, third-party APIs, etc.
    /// </summary>
    Task ExecuteExternalApiOperationAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an external API call with retry, circuit breaker, and timeout (30s).
    /// Returns the result of the operation.
    /// </summary>
    Task<T> ExecuteExternalApiOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
