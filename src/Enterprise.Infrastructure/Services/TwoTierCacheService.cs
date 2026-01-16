using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Enterprise.Infrastructure.Services;

/// <summary>
/// Two-tier cache implementation with in-memory (L1) and Redis (L2)
/// L1 provides ultra-fast access for hot data
/// L2 provides distributed caching across multiple instances
/// </summary>
public class TwoTierCacheService : ITwoTierCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<TwoTierCacheService> _logger;

    // Cache statistics
    private long _l1Hits;
    private long _l1Misses;
    private long _l2Hits;
    private long _l2Misses;

    // Default expirations
    private static readonly TimeSpan DefaultL1Expiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultL2Expiration = TimeSpan.FromMinutes(15);

    public TwoTierCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<TwoTierCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Try L1 cache first (in-memory - fastest)
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            Interlocked.Increment(ref _l1Hits);
            _logger.LogDebug("L1 cache hit for key: {Key}", key);
            return cachedValue;
        }

        Interlocked.Increment(ref _l1Misses);

        // Try L2 cache (Redis - distributed)
        var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(distributedValue))
        {
            Interlocked.Increment(ref _l2Hits);
            _logger.LogDebug("L2 cache hit for key: {Key}", key);

            var value = JsonSerializer.Deserialize<T>(distributedValue);

            // Populate L1 cache with L2 value
            if (value != null)
            {
                _memoryCache.Set(key, value, DefaultL1Expiration);
            }

            return value;
        }

        Interlocked.Increment(ref _l2Misses);
        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? l1Expiration = null,
        TimeSpan? l2Expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (value == null)
        {
            _logger.LogWarning("Attempted to cache null value for key: {Key}", key);
            return;
        }

        var l1Exp = l1Expiration ?? DefaultL1Expiration;
        var l2Exp = l2Expiration ?? DefaultL2Expiration;

        // Set in L1 cache (in-memory)
        _memoryCache.Set(key, value, l1Exp);

        // Set in L2 cache (Redis)
        var serializedValue = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = l2Exp
        };

        await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);

        _logger.LogDebug("Cached value for key: {Key} (L1: {L1Expiration}, L2: {L2Expiration})",
            key, l1Exp, l2Exp);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        // Remove from both caches
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);

        _logger.LogDebug("Removed key from cache: {Key}", key);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? l1Expiration = null,
        TimeSpan? l2Expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Generate value using factory
        _logger.LogDebug("Cache miss, generating value for key: {Key}", key);
        var value = await factory();

        // Cache the generated value
        if (value != null)
        {
            await SetAsync(key, value, l1Expiration, l2Expiration, cancellationToken);
        }

        return value;
    }

    public void ClearL1Cache()
    {
        // MemoryCache doesn't have a built-in Clear method
        // This would require tracking keys or using a custom implementation
        // For now, log a warning
        _logger.LogWarning("L1 cache clear requested but not fully supported by IMemoryCache");

        // Option: Create a new MemoryCache instance (requires DI changes)
        // or track all keys manually
    }

    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            L1Hits = Interlocked.Read(ref _l1Hits),
            L1Misses = Interlocked.Read(ref _l1Misses),
            L2Hits = Interlocked.Read(ref _l2Hits),
            L2Misses = Interlocked.Read(ref _l2Misses),
            L1EntryCount = 0 // MemoryCache doesn't expose entry count
        };
    }
}
