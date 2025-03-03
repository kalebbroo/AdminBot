using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text.Json;
using AdminBot.Utils;

namespace AdminBot.Services;

/// <summary>
/// Provides caching functionality with Redis and in-memory fallback.
/// Implements distributed caching patterns for the Discord bot.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<CacheService> _logger;

    // Backup cache for when Redis is unavailable
    private readonly MemoryCache _memoryCache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger)
    {
        _redis = redis;
        _logger = logger;
        _memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1024 // Limit size to prevent memory issues
        });

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Gets a value from cache with type safety.
    /// Falls back to memory cache if Redis is unavailable.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cache = _redis.GetDatabase();
            var value = await cache.StringGetAsync(key);

            if (!value.HasValue)
            {
                // Try memory cache as fallback
                if (_memoryCache.TryGetValue(key, out T memoryValue))
                {
                    return memoryValue;
                }
                return default;
            }

            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis unavailable, falling back to memory cache for key: {key} Error: {ex}");

            // Fallback to memory cache
            if (_memoryCache.TryGetValue(key, out T memoryValue))
            {
                return memoryValue;
            }
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Redis unavailable, falling back to memory cache for key: {key} Error: {ex}");
            return default;
        }
    }

    /// <summary>
    /// Sets a value in cache with optional expiry.
    /// Attempts to set in both Redis and memory cache.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var cache = _redis.GetDatabase();

            // Set in Redis
            await cache.StringSetAsync(
                key,
                serializedValue,
                expiry ?? TimeSpan.FromHours(1)
            );

            // Also set in memory cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSize(1) // Each entry counts as 1 towards size limit
                .SetAbsoluteExpiration(expiry ?? TimeSpan.FromHours(1));

            _memoryCache.Set(key, value, cacheOptions);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis unavailable, falling back to memory cache for key: {key} Error: {ex}");

            // Set only in memory cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSize(1)
                .SetAbsoluteExpiration(expiry ?? TimeSpan.FromHours(1));

            _memoryCache.Set(key, value, cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Redis unavailable, falling back to memory cache for key: {key} Error: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Gets a value from cache or creates it if it doesn't exist.
    /// Uses semaphore to prevent multiple simultaneous creations.
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        // Try to get from cache first
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        // Use semaphore to prevent multiple simultaneous creations
        await _semaphore.WaitAsync();
        try
        {
            // Double check after acquiring semaphore
            cached = await GetAsync<T>(key);
            if (cached != null)
            {
                return cached;
            }

            // Create new value
            var value = await factory();

            // Cache the value
            await SetAsync(key, value, expiry);

            return value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes a value from both Redis and memory cache.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            var cache = _redis.GetDatabase();
            await cache.KeyDeleteAsync(key);
            _memoryCache.Remove(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis unavailable, falling back to memory cache for key: {key} Error: {ex}");
            _memoryCache.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Redis unavailable, falling back to memory cache for key: {key} Error: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Checks if a key exists in either Redis or memory cache.
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var cache = _redis.GetDatabase();
            if (await cache.KeyExistsAsync(key))
            {
                return true;
            }
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError($"Redis unavailable, falling back to memory cache for key: {key} Error: {ex}");
        }

        // Check memory cache as fallback
        return _memoryCache.TryGetValue(key, out _);
    }

    /// <summary>
    /// Handles cleanup when the service is disposed.
    /// </summary>
    public void Dispose()
    {
        _memoryCache.Dispose();
        _semaphore.Dispose();
    }
}