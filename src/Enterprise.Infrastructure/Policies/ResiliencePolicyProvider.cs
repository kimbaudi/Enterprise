using Enterprise.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Enterprise.Infrastructure.Policies;

/// <summary>
/// Provides Polly resilience policies for various operations in the application.
/// Implements circuit breaker, retry, and timeout patterns for external dependencies.
/// </summary>
public class ResiliencePolicyProvider : IResiliencePolicyProvider
{
    private readonly ILogger<ResiliencePolicyProvider> _logger;

    public ResiliencePolicyProvider(ILogger<ResiliencePolicyProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Database policy: Retry 3 times with exponential backoff + circuit breaker.
    /// Circuit opens after 50% failure rate over 10s sampling (min 5 requests).
    /// </summary>
    private IAsyncPolicy DatabasePolicy => Policy
        .Handle<InvalidOperationException>()
        .Or<TimeoutException>()
        .Or<SqlException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning(exception,
                    "Database operation retry {RetryCount} after {Delay}s. Exception: {ExceptionMessage}",
                    retryCount, timeSpan.TotalSeconds, exception.Message);
            })
        .WrapAsync(Policy
            .Handle<InvalidOperationException>()
            .Or<TimeoutException>()
            .Or<SqlException>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,
                samplingDuration: TimeSpan.FromSeconds(10),
                minimumThroughput: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(exception,
                        "Database circuit breaker opened for {Duration}s. Exception: {ExceptionMessage}",
                        duration.TotalSeconds, exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Database circuit breaker reset - connection restored");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Database circuit breaker half-open - testing connection");
                }));

    /// <summary>
    /// Cache (Redis) policy: Retry 2 times with exponential backoff + circuit breaker.
    /// Circuit opens after 50% failure rate over 10s sampling (min 3 requests), stays open 20s.
    /// Less aggressive than database policy since cache failures are not critical.
    /// </summary>
    private IAsyncPolicy CachePolicy => Policy
        .Handle<Exception>(ex =>
            ex is not ArgumentNullException &&
            ex is not ArgumentException &&
            ex is not InvalidOperationException)
        .WaitAndRetryAsync(
            retryCount: 2,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning(exception,
                    "Cache operation retry {RetryCount} after {Delay}s. Exception: {ExceptionMessage}",
                    retryCount, timeSpan.TotalSeconds, exception.Message);
            })
        .WrapAsync(Policy
            .Handle<Exception>(ex =>
                ex is not ArgumentNullException &&
                ex is not ArgumentException &&
                ex is not InvalidOperationException)
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,
                samplingDuration: TimeSpan.FromSeconds(10),
                minimumThroughput: 3,
                durationOfBreak: TimeSpan.FromSeconds(20),
                onBreak: (exception, duration) =>
                {
                    _logger.LogWarning(exception,
                        "Cache circuit breaker opened for {Duration}s. Exception: {ExceptionMessage}. Cache will be bypassed.",
                        duration.TotalSeconds, exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Cache circuit breaker reset - cache connection restored");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Cache circuit breaker half-open - testing cache connection");
                }));

    /// <summary>
    /// External API policy: Retry + circuit breaker + timeout (30s).
    /// Circuit opens after 50% failure rate over 10s sampling (min 5 requests), stays open 60s.
    /// </summary>
    private IAsyncPolicy ExternalApiPolicy
    {
        get
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "External API retry {RetryCount} after {Delay}s. Exception: {ExceptionMessage}",
                            retryCount, timeSpan.TotalSeconds, exception.Message);
                    });

            var circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<TimeoutException>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5,
                    samplingDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 5,
                    durationOfBreak: TimeSpan.FromSeconds(60),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogError(exception,
                            "External API circuit breaker opened for {Duration}s. Exception: {ExceptionMessage}",
                            duration.TotalSeconds, exception.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("External API circuit breaker reset - service restored");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("External API circuit breaker half-open - testing service");
                    });

            var timeoutPolicy = Policy
                .TimeoutAsync(
                    timeout: TimeSpan.FromSeconds(30),
                    onTimeoutAsync: (context, timeSpan, task) =>
                    {
                        _logger.LogWarning("External API timeout after {Timeout}s", timeSpan.TotalSeconds);
                        return Task.CompletedTask;
                    });

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
        }
    }

    // Interface implementation methods
    public async Task<T> ExecuteDatabaseOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return await DatabasePolicy.ExecuteAsync(async (ct) => await operation(), cancellationToken);
    }

    public async Task<T?> ExecuteCacheOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            return await CachePolicy.ExecuteAsync(async (ct) => await operation(), cancellationToken);
        }
        catch (Exception ex)
        {
            // Graceful degradation - cache failures should not break the app
            _logger.LogWarning(ex, "Cache operation failed after all retries. Returning default value.");
            return default;
        }
    }

    public async Task ExecuteExternalApiOperationAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExternalApiPolicy.ExecuteAsync(async (ct) => await operation(), cancellationToken);
    }

    public async Task<T> ExecuteExternalApiOperationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return await ExternalApiPolicy.ExecuteAsync(async (ct) => await operation(), cancellationToken);
    }
}
