namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Two-tier cache service with in-memory (L1) and distributed Redis (L2) caching
/// Provides faster access for hot data while maintaining distributed cache benefits
/// </summary>
public interface ITwoTierCacheService
{
    /// <summary>
    /// Gets a value from cache (checks L1 first, then L2)
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in both L1 and L2 cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? l1Expiration = null, TimeSpan? l2Expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from both L1 and L2 cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a cached value (checks L1, then L2, then factory)
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? l1Expiration = null,
        TimeSpan? l2Expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all items from L1 cache (L2 remains intact)
    /// </summary>
    void ClearL1Cache();

    /// <summary>
    /// Gets cache statistics (hit rates, entry counts)
    /// </summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public class CacheStatistics
{
    public long L1Hits { get; set; }
    public long L1Misses { get; set; }
    public long L2Hits { get; set; }
    public long L2Misses { get; set; }
    public int L1EntryCount { get; set; }
    public double L1HitRate => L1Hits + L1Misses > 0 ? (double)L1Hits / (L1Hits + L1Misses) * 100 : 0;
    public double L2HitRate => L2Hits + L2Misses > 0 ? (double)L2Hits / (L2Hits + L2Misses) * 100 : 0;
}
