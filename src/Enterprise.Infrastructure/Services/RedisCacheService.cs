using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Enterprise.Infrastructure.Services;

/// <summary>
/// Redis-based distributed cache implementation with circuit breaker resilience
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IResiliencePolicyProvider? _policyProvider;
    private readonly DistributedCacheEntryOptions _defaultOptions;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger,
        IResiliencePolicyProvider? policyProvider = null)
    {
        _cache = cache;
        _logger = logger;
        _policyProvider = policyProvider;
        _defaultOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (_policyProvider != null)
            {
                return await _policyProvider.ExecuteCacheOperationAsync<T>(async () =>
                {
                    var cachedData = await _cache.GetStringAsync(key, cancellationToken);

                    if (string.IsNullOrEmpty(cachedData))
                        return null!;

                    return JsonSerializer.Deserialize<T>(cachedData)!;
                }, cancellationToken);
            }
            else
            {
                var cachedData = await _cache.GetStringAsync(key, cancellationToken);

                if (string.IsNullOrEmpty(cachedData))
                    return null;

                return JsonSerializer.Deserialize<T>(cachedData);
            }
        }
        catch (Exception ex)
        {
            // Log cache failure but don't propagate - graceful degradation
            _logger.LogWarning(ex, "Cache GET failed for key {Key}. Returning null (cache miss).", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (_policyProvider != null)
            {
                await _policyProvider.ExecuteCacheOperationAsync(async () =>
                {
                    var options = expiration.HasValue
                        ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration.Value }
                        : _defaultOptions;

                    var serializedData = JsonSerializer.Serialize(value);
                    await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
                    return new object(); // Return non-null object for ExecuteCacheOperationAsync
                }, cancellationToken);
            }
            else
            {
                var options = expiration.HasValue
                    ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration.Value }
                    : _defaultOptions;

                var serializedData = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log cache failure but don't propagate - graceful degradation
            _logger.LogWarning(ex, "Cache SET failed for key {Key}. Data not cached.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_policyProvider != null)
            {
                await _policyProvider.ExecuteCacheOperationAsync(async () =>
                {
                    await _cache.RemoveAsync(key, cancellationToken);
                    return new object(); // Return non-null object for ExecuteCacheOperationAsync
                }, cancellationToken);
            }
            else
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {Key}.", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based deletion requires Redis-specific implementation
        // For now, we'll implement basic removal. Full implementation would need IConnectionMultiplexer
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_policyProvider != null)
            {
                // ExecuteCacheOperationAsync only works with reference types, so we use string as an indicator
                var result = await _policyProvider.ExecuteCacheOperationAsync(async () =>
                {
                    var value = await _cache.GetStringAsync(key, cancellationToken);
                    // Return empty string instead of null to satisfy non-nullable return
                    return value ?? string.Empty;
                }, cancellationToken);
                return !string.IsNullOrEmpty(result);
            }
            else
            {
                var value = await _cache.GetStringAsync(key, cancellationToken);
                return !string.IsNullOrEmpty(value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS check failed for key {Key}. Returning false.", key);
            return false;
        }
    }
}
