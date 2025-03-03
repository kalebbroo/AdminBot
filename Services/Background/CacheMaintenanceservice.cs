using AdminBot.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

namespace AdminBot.Services.Background;

/// <summary>Background service that maintains cache health and performs optimization.
/// Handles cache cleanup, monitoring, and statistics tracking.</summary>
public class CacheMaintenanceService(ILogger<CacheMaintenanceService> logger, IConnectionMultiplexer redis, ICacheService cache) : BackgroundService
{
    private readonly ILogger<CacheMaintenanceService> _logger = logger;
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly ICacheService _cache = cache;
    private readonly ConcurrentDictionary<string, CacheStats> _statistics = new ConcurrentDictionary<string, CacheStats>();
    private readonly TimeSpan _maintenanceInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _statsInterval = TimeSpan.FromSeconds(30);
    private readonly int _maxKeysPerScan = 1000;

    /// <summary>Main execution loop for the maintenance service.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache maintenance service starting");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await PerformMaintenanceAsync(stoppingToken);
                await Task.Delay(_maintenanceInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cache maintenance service");
        }
        finally
        {
            _logger.LogInformation("Cache maintenance service stopping");
        }
    }

    /// <summary>Performs maintenance tasks on the cache.</summary>
    private async Task PerformMaintenanceAsync(CancellationToken stoppingToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var server = GetPreferredServer();
            if (server == null)
            {
                _logger.LogWarning("No Redis server available for maintenance");
                return;
            }
            // Collect statistics
            await CollectStatisticsAsync(server);
            // Check memory usage
            var info = await server.InfoAsync("memory");
            var usedMemory = ParseMemoryInfo(info);
            if (usedMemory > 0)
            {
                var keyCount = server.Keys().Count();
                var memoryPercent = usedMemory / 1024.0 / 1024.0 * 100;
                _logger.LogInformation(
                    "Redis memory usage: {UsedMemory:N2}MB ({Percent:N1}%)",
                    usedMemory / 1024.0 / 1024.0,
                    memoryPercent);

                // If memory usage is high, trigger cleanup
                if (memoryPercent > 80)
                {
                    await CleanupCacheAsync(server, stoppingToken);
                }
            }

            // Clean up expired keys
            await CleanupExpiredKeysAsync(server, stoppingToken);

            // Update statistics
            await UpdateStatisticsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache maintenance cycle");
        }
    }

    /// <summary>
    /// Collects statistics about cache usage.
    /// </summary>
    private async Task CollectStatisticsAsync(IServer server)
    {
        var now = DateTime.UtcNow;
        var keyCount = server.Keys().Count();

        foreach (var prefix in GetMonitoredPrefixes())
        {
            var keys = server.Keys(pattern: $"{prefix}:*", pageSize: _maxKeysPerScan);
            var count = keys.Count();

            _statistics.AddOrUpdate(
                prefix,
                new CacheStats { KeyCount = count, LastUpdate = now },
                (_, existing) => new CacheStats
                {
                    KeyCount = count,
                    LastUpdate = now,
                    HitCount = existing.HitCount,
                    MissCount = existing.MissCount
                });
        }

        _logger.LogDebug(
            "Cache statistics collected: {TotalKeys} total keys",
            keyCount);
    }

    /// <summary>
    /// Cleans up expired keys from the cache.
    /// </summary>
    private async Task CleanupExpiredKeysAsync(IServer server, CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();
        var deletedCount = 0;

        foreach (var prefix in GetMonitoredPrefixes())
        {
            var keys = server.Keys(pattern: $"{prefix}:*", pageSize: _maxKeysPerScan);

            foreach (var key in keys)
            {
                // Check if key has TTL
                var ttl = await db.KeyTimeToLiveAsync(key);
                if (!ttl.HasValue) continue;

                // Delete if expired
                if (ttl.Value <= TimeSpan.Zero)
                {
                    await db.KeyDeleteAsync(key);
                    deletedCount++;
                }

                if (deletedCount % 1000 == 0 && deletedCount > 0)
                {
                    _logger.LogInformation(
                        "Cleaned up {Count} expired keys",
                        deletedCount);
                }
            }
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Cache cleanup completed: removed {Count} expired keys",
                deletedCount);
        }
    }

    /// <summary>
    /// Performs cleanup when memory usage is high.
    /// </summary>
    private async Task CleanupCacheAsync(IServer server, CancellationToken stoppingToken)
    {
        _logger.LogWarning("High memory usage detected, performing cache cleanup");

        var db = _redis.GetDatabase();
        var deletedCount = 0;

        // Get keys sorted by last access time
        var allKeys = server.Keys(pattern: "*", pageSize: _maxKeysPerScan).ToArray();
        var keyIdleTimes = new List<(RedisKey Key, TimeSpan IdleTime)>();

        // First collect all idle times
        foreach (var key in allKeys)
        {
            var idleTime = await db.KeyIdleTimeAsync(key);
            if (idleTime.HasValue)
            {
                keyIdleTimes.Add((key, idleTime.Value));
            }
            else
            {
                keyIdleTimes.Add((key, TimeSpan.Zero));
            }
        }

        // Then sort by idle time
        var sortedKeys = keyIdleTimes
            .OrderByDescending(k => k.IdleTime);

        foreach (var item in sortedKeys)
        {
            // Delete keys idle for more than an hour
            if (item.IdleTime > TimeSpan.FromHours(1))
            {
                await db.KeyDeleteAsync(item.Key);
                deletedCount++;
            }

            // Check if we've freed up enough space
            if (deletedCount % 1000 == 0)
            {
                var info = await server.InfoAsync("memory");
                var memoryPercent = ParseMemoryInfo(info) / 1024.0 / 1024.0 * 100;

                if (memoryPercent < 70)
                {
                    break;
                }
            }
        }

        _logger.LogInformation(
            "Memory cleanup completed: removed {Count} idle keys",
            deletedCount);
    }

    /// <summary>
    /// Updates cache statistics for monitoring.
    /// </summary>
    private async Task UpdateStatisticsAsync(CancellationToken stoppingToken)
    {
        foreach (var (prefix, stats) in _statistics)
        {
            // Remove old stats
            if (DateTime.UtcNow - stats.LastUpdate > TimeSpan.FromHours(1))
            {
                _statistics.TryRemove(prefix, out _);
                continue;
            }

            _logger.LogDebug(
                "Cache stats for {Prefix}: {KeyCount} keys, {HitRate:P2} hit rate",
                prefix,
                stats.KeyCount,
                stats.HitRate);
        }

        await Task.Delay(_statsInterval, stoppingToken);
    }

    /// <summary>
    /// Gets the preferred Redis server for operations.
    /// </summary>
    private IServer? GetPreferredServer()
    {
        var endpoints = _redis.GetEndPoints();
        return endpoints.Length > 0 ? _redis.GetServer(endpoints[0]) : null;
    }

    /// <summary>
    /// Parses memory info from Redis INFO command.
    /// </summary>
    private static long ParseMemoryInfo(object info)
    {
        // Convert the info object to string
        string infoStr = info.ToString() ?? "";
        
        foreach (var line in infoStr.Split('\n'))
        {
            if (line.StartsWith("used_memory:"))
            {
                return long.TryParse(line.Split(':')[1], out var memory) ? memory : 0;
            }
        }
        return 0;
    }

    /// <summary>
    /// Gets the cache key prefixes to monitor.
    /// </summary>
    private static IEnumerable<string> GetMonitoredPrefixes()
    {
        yield return "profile";
        yield return "guild";
        yield return "event";
        yield return "warning";
        yield return "session";
        yield return "stats";
    }
}

/// <summary>
/// Tracks statistics for a cache prefix.
/// </summary>
public class CacheStats
{
    public long KeyCount { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public DateTime LastUpdate { get; set; }

    public double HitRate =>
        HitCount + MissCount > 0
            ? (double)HitCount / (HitCount + MissCount)
            : 0;
}