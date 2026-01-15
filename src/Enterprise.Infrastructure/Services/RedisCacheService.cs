using Enterprise.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Enterprise.Infrastructure.Services;

/// <summary>
/// Redis-based distributed cache implementation
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _defaultOptions;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
        _defaultOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cachedData = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(cachedData))
            return null;

        return JsonSerializer.Deserialize<T>(cachedData);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = expiration.HasValue
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration.Value }
            : _defaultOptions;

        var serializedData = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based deletion requires Redis-specific implementation
        // For now, we'll implement basic removal. Full implementation would need IConnectionMultiplexer
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return !string.IsNullOrEmpty(value);
    }
}
