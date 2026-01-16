using Asp.Versioning;
using Enterprise.Application.Common.Interfaces;
using Enterprise.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.WebApi.Controllers;

/// <summary>
/// Cache management and monitoring controller
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class CacheController : ControllerBase
{
    private readonly ITwoTierCacheService _twoTierCache;
    private readonly ICacheService _redisCache;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ITwoTierCacheService twoTierCache,
        ICacheService redisCache,
        ILogger<CacheController> logger)
    {
        _twoTierCache = twoTierCache;
        _redisCache = redisCache;
        _logger = logger;
    }

    /// <summary>
    /// Get cache statistics (hit rates, entry counts)
    /// </summary>
    [HttpGet("statistics")]
    public ActionResult<ApiResponse<CacheStatistics>> GetStatistics()
    {
        var stats = _twoTierCache.GetStatistics();
        return Ok(new ApiResponse<CacheStatistics>(stats));
    }

    /// <summary>
    /// Clear L1 (in-memory) cache
    /// </summary>
    [HttpPost("clear/l1")]
    public ActionResult<ApiResponse<string>> ClearL1Cache()
    {
        _twoTierCache.ClearL1Cache();
        _logger.LogInformation("L1 cache cleared by user: {User}", User.Identity?.Name);
        return Ok(new ApiResponse<string>("L1 cache cleared successfully"));
    }

    /// <summary>
    /// Clear specific cache key from both L1 and L2
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<ActionResult<ApiResponse<string>>> ClearKey(string key, CancellationToken cancellationToken)
    {
        await _twoTierCache.RemoveAsync(key, cancellationToken);
        _logger.LogInformation("Cache key cleared: {Key} by user: {User}", key, User.Identity?.Name);
        return Ok(new ApiResponse<string>($"Cache key '{key}' cleared successfully"));
    }

    /// <summary>
    /// Test cache performance (L1 vs L2 vs no cache)
    /// </summary>
    [HttpGet("test-performance")]
    public async Task<ActionResult<ApiResponse<CachePerformanceTestResult>>> TestCachePerformance(CancellationToken cancellationToken)
    {
        var testKey = $"perf-test-{Guid.NewGuid()}";
        var testValue = new { Data = "Performance test data", Timestamp = DateTime.UtcNow };

        // Test 1: No cache (generate data)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(10, cancellationToken); // Simulate work
        var noCacheTime = sw.ElapsedMilliseconds;

        // Test 2: L2 cache (Redis)
        await _twoTierCache.SetAsync(testKey, testValue,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), cancellationToken);

        // Clear L1 to force L2 hit
        _twoTierCache.ClearL1Cache();

        sw.Restart();
        await _twoTierCache.GetAsync<object>(testKey, cancellationToken);
        var l2Time = sw.ElapsedMilliseconds;

        // Test 3: L1 cache (in-memory)
        sw.Restart();
        await _twoTierCache.GetAsync<object>(testKey, cancellationToken);
        var l1Time = sw.ElapsedMilliseconds;

        // Cleanup
        await _twoTierCache.RemoveAsync(testKey, cancellationToken);

        return Ok(new ApiResponse<CachePerformanceTestResult>(new CachePerformanceTestResult
        {
            NoCacheMs = noCacheTime,
            L2CacheMs = l2Time,
            L1CacheMs = l1Time,
            L1Speedup = noCacheTime > 0 ? (double)noCacheTime / Math.Max(l1Time, 1) : 0,
            L2Speedup = noCacheTime > 0 ? (double)noCacheTime / Math.Max(l2Time, 1) : 0
        }));
    }
}

public class CachePerformanceTestResult
{
    public long NoCacheMs { get; set; }
    public long L2CacheMs { get; set; }
    public long L1CacheMs { get; set; }
    public double L1Speedup { get; set; }
    public double L2Speedup { get; set; }
}
