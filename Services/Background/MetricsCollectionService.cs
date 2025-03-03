using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NetCord;
using NetCord.Gateway;
using AdminBot.Services;
using AdminBot.Models;
using System.Collections.Generic;

namespace AdminBot.Services.Background;

/// <summary>
/// Background service that collects and tracks bot metrics and performance statistics.
/// Provides insights into system health, usage patterns, and resource utilization.
/// </summary>
public class MetricsCollectionService : BackgroundService
{
    private readonly ILogger<MetricsCollectionService> _logger;
    private readonly DatabaseService _database;
    private readonly ICacheService _cache;
    private readonly BotGatewayClient _gateway;
    private readonly ConcurrentDictionary<string, MetricCounter> _counters;
    private readonly ConcurrentDictionary<string, Stopwatch> _timers;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromMinutes(1);
    private readonly Process _currentProcess;

    public MetricsCollectionService(
        ILogger<MetricsCollectionService> logger,
        DatabaseService database,
        ICacheService cache,
        BotGatewayClient gateway)
    {
        _logger = logger;
        _database = database;
        _cache = cache;
        _gateway = gateway;
        _counters = new ConcurrentDictionary<string, MetricCounter>();
        _timers = new ConcurrentDictionary<string, Stopwatch>();
        _currentProcess = Process.GetCurrentProcess();
    }

    /// <summary>
    /// Main execution loop for metrics collection.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics collection service starting");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CollectMetricsAsync(stoppingToken);
                await Task.Delay(_collectionInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in metrics collection service");
        }
        finally
        {
            _logger.LogInformation("Metrics collection service stopping");
        }
    }

    /// <summary>
    /// Collects various metrics about the bot's performance and usage.
    /// </summary>
    private async Task CollectMetricsAsync(CancellationToken stoppingToken)
    {
        try
        {
            var timestamp = DateTime.UtcNow;
            var metrics = new Dictionary<string, object>();

            // Collect system metrics
            CollectSystemMetrics(metrics);

            // Collect Discord metrics
            await CollectDiscordMetricsAsync(metrics);

            // Collect performance metrics
            CollectPerformanceMetrics(metrics);

            // Collect custom counters
            CollectCustomMetrics(metrics, timestamp);

            // Store metrics in database
            await _database.SaveMetricsAsync(new BotMetrics
            {
                Timestamp = timestamp,
                Metrics = metrics
            });

            // Update cache for quick access
            await _cache.SetAsync(
                "latest_metrics",
                metrics,
                TimeSpan.FromMinutes(5));

            _logger.LogDebug("Metrics collection completed: {Count} metrics collected", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metrics");
        }
    }

    /// <summary>
    /// Collects system-level metrics.
    /// </summary>
    private void CollectSystemMetrics(Dictionary<string, object> metrics)
    {
        metrics["process_cpu_time"] = _currentProcess.TotalProcessorTime.TotalMilliseconds;
        metrics["process_memory"] = _currentProcess.WorkingSet64;
        metrics["process_threads"] = _currentProcess.Threads.Count;
        metrics["process_handles"] = _currentProcess.HandleCount;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            CollectLinuxMetrics(metrics);
        }
    }

    /// <summary>
    /// Collects Linux-specific metrics.
    /// </summary>
    private void CollectLinuxMetrics(Dictionary<string, object> metrics)
    {
        try
        {
            var memInfo = File.ReadAllText("/proc/meminfo");
            var cpuInfo = File.ReadAllText("/proc/stat");

            // Parse memory info
            foreach (var line in memInfo.Split('\n'))
            {
                if (line.StartsWith("MemTotal:"))
                {
                    metrics["system_memory_total"] = ParseLinuxMemoryValue(line);
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    metrics["system_memory_available"] = ParseLinuxMemoryValue(line);
                }
            }

            // Parse CPU info
            var cpuLines = cpuInfo.Split('\n');
            if (cpuLines.Length > 0)
            {
                var cpuTotal = ParseLinuxCpuStats(cpuLines[0]);
                metrics["system_cpu_total"] = cpuTotal;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error collecting Linux metrics");
        }
    }

    /// <summary>
    /// Collects Discord-related metrics.
    /// </summary>
    private async Task CollectDiscordMetricsAsync(Dictionary<string, object> metrics)
    {
        try
        {
            // Get guild and user counts
            metrics["total_guilds"] = _gateway.GuildCount;
            
            var guilds = _gateway.GetGuilds();
            int totalUsers = 0;
            foreach (var guild in guilds)
            {
                if (guild != null)
                {
                    var guildMemberCount = guild.UserCount;
                    totalUsers += guildMemberCount;
                }
            }
            metrics["total_users"] = totalUsers;
            
            // Get latency
            metrics["websocket_latency"] = _gateway.Latency.TotalMilliseconds;
            
            // Get session info
            metrics["gateway_session_id"] = _gateway.SessionId ?? "unknown";

            // Get shard info
            var shardInfo = _gateway.ShardInfo;
            if (shardInfo != null)
            {
                metrics["shard_id"] = shardInfo.Values.FirstOrDefault()?.Id;
                metrics["shard_count"] = shardInfo.Count;
            }

            // Collect voice connection metrics
            var voiceSessions = await _database.GetActiveVoiceSessionsAsync();
            metrics["voice_connections"] = voiceSessions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error collecting Discord metrics");
        }
    }

    /// <summary>
    /// Collects performance-related metrics.
    /// </summary>
    private void CollectPerformanceMetrics(Dictionary<string, object> metrics)
    {
        // Collect timer metrics
        foreach (var (name, timer) in _timers)
        {
            metrics[$"timer_{name}_ms"] = timer.ElapsedMilliseconds;
        }

        // Collect GC metrics
        metrics["gc_total_memory"] = GC.GetTotalMemory(false);
        metrics["gc_collection_count_0"] = GC.CollectionCount(0);
        metrics["gc_collection_count_1"] = GC.CollectionCount(1);
        metrics["gc_collection_count_2"] = GC.CollectionCount(2);
    }

    /// <summary>
    /// Collects custom counter metrics.
    /// </summary>
    private void CollectCustomMetrics(Dictionary<string, object> metrics, DateTime timestamp)
    {
        foreach (var (name, counter) in _counters)
        {
            var value = counter.GetAndReset();
            metrics[$"counter_{name}"] = value;

            // Store rate metrics
            if (counter.LastReset.HasValue)
            {
                var duration = (timestamp - counter.LastReset.Value).TotalSeconds;
                if (duration > 0)
                {
                    metrics[$"rate_{name}_per_second"] = value / duration;
                }
            }
        }
    }

    /// <summary>
    /// Increments a named counter.
    /// </summary>
    public void IncrementCounter(string name, long value = 1)
    {
        _counters.AddOrUpdate(
            name,
            new MetricCounter { Value = value },
            (_, existing) =>
            {
                existing.Value += value;
                return existing;
            });
    }

    /// <summary>
    /// Starts a named timer.
    /// </summary>
    public IDisposable StartTimer(string name)
    {
        var timer = _timers.GetOrAdd(name, _ => new Stopwatch());
        timer.Start();
        return new TimerScope(timer);
    }

    /// <summary>
    /// Parses memory values from Linux /proc/meminfo.
    /// </summary>
    private static long ParseLinuxMemoryValue(string line)
    {
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && long.TryParse(parts[1], out var value))
        {
            return value * 1024; // Convert KB to bytes
        }
        return 0;
    }

    /// <summary>
    /// Parses CPU stats from Linux /proc/stat.
    /// </summary>
    private static long ParseLinuxCpuStats(string line)
    {
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var total = 0L;
        for (var i = 1; i < parts.Length; i++)
        {
            if (long.TryParse(parts[i], out var value))
            {
                total += value;
            }
        }
        return total;
    }
}

/// <summary>
/// Tracks a metric counter with reset capability.
/// </summary>
public class MetricCounter
{
    public long Value { get; set; }
    public DateTime? LastReset { get; private set; }

    public long GetAndReset()
    {
        var value = Value;
        Value = 0;
        LastReset = DateTime.UtcNow;
        return value;
    }
}

/// <summary>
/// Scope for automatic timer stop on disposal.
/// </summary>
public class TimerScope : IDisposable
{
    private readonly Stopwatch _timer;

    public TimerScope(Stopwatch timer)
    {
        _timer = timer;
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}