using Microsoft.Extensions.Logging;
using Supabase;
using System.Text.Json;
using AdminBot.Models;
using System.Collections.Concurrent;
using NetCord;
using NetCord.Gateway;

namespace AdminBot.Services;

/// <summary>
/// Core database service that handles all direct Supabase operations.
/// Provides thread-safe database access with retry logic and error handling.
/// </summary>
public sealed class DatabaseService : IAsyncDisposable
{
    private readonly Client _supabase;
    private readonly ICacheService _cache;
    private readonly ILogger<DatabaseService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tableLocks;

    // Retry configuration
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public DatabaseService(
        Client supabase,
        ICacheService cache,
        ILogger<DatabaseService> logger)
    {
        _supabase = supabase;
        _cache = cache;
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
        _tableLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region Core Database Operations

    /// <summary>
    /// Executes a database operation with retry logic and error handling.
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt} of {MaxAttempts} failed for operation {Operation}",
                    attempt,
                    MaxRetryAttempts,
                    operationName);

                await Task.Delay(RetryDelay * attempt, cancellationToken).ConfigureAwait(false);
            }
        }

        // Final attempt
        return await operation().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a table-specific lock for concurrent operations.
    /// </summary>
    private SemaphoreSlim GetTableLock(string tableName)
    {
        return _tableLocks.GetOrAdd(tableName, _ => new SemaphoreSlim(1, 1));
    }

    #endregion

    #region User Profile Operations

    /// <summary>
    /// Gets or creates a user profile with proper locking and caching.
    /// </summary>
    public async Task<UserProfile> GetOrCreateUserProfileAsync(ulong userId, ulong guildId, CancellationToken cancellationToken = default)
    {
        string userIdStr = userId.ToString();
        string guildIdStr = guildId.ToString();
        string cacheKey = $"profile:{userIdStr}:{guildIdStr}";

        try
        {
            // Try cache first
            if (await _cache.GetAsync<UserProfile>(cacheKey).ConfigureAwait(false) is UserProfile cached)
            {
                return cached;
            }

            // Get table lock
            SemaphoreSlim tableLock = GetTableLock("guild_members");
            await tableLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Check database
                var profile = await ExecuteWithRetryAsync(
                    async () => await _supabase
                        .From<UserProfile>()
                        .Where(p => p.Id == userIdStr)
                        .Single(),
                    "GetUserProfile",
                    cancellationToken)
                    .ConfigureAwait(false);

                if (profile is null)
                {
                    // Create new profile
                    profile = new UserProfile
                    {
                        Id = userIdStr,
                        GuildProfiles = new List<GuildProfile>
                        {
                            new GuildProfile
                            {
                                GuildId = guildIdStr,
                                Level = 0,
                                Xp = 0,
                                MessagesSent = 0,
                                VoiceMinutes = 0,
                                CreatedAt = DateTime.UtcNow,
                                LastActive = DateTime.UtcNow
                            }
                        },
                        CreatedAt = DateTime.UtcNow,
                        LastActive = DateTime.UtcNow
                    };

                    await ExecuteWithRetryAsync(
                        async () => await _supabase
                            .From<UserProfile>()
                            .Insert(profile),
                        "CreateUserProfile",
                        cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation(
                        "Created new profile for user {UserId} in guild {GuildId}",
                        userId,
                        guildId);
                }

                // Cache result
                await _cache.SetAsync(
                    cacheKey,
                    profile,
                    TimeSpan.FromMinutes(5))
                    .ConfigureAwait(false);

                return profile;
            }
            finally
            {
                tableLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get/create profile for user {UserId} in guild {GuildId}",
                userId,
                guildId);
            throw;
        }
    }

    /// <summary>
    /// Updates a user profile with optimistic concurrency.
    /// </summary>
    public async Task UpdateUserProfileAsync(
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"profile:{profile.Id}:{profile.GuildProfiles}";

        try
        {
            await ExecuteWithRetryAsync(
                async () => await _supabase
                    .From<UserProfile>()
                    .Where(p => p.Id == profile.Id && p.GuildProfiles == profile.GuildProfiles)
                    .Update(profile),
                "UpdateUserProfile",
                cancellationToken)
                .ConfigureAwait(false);

            // Clear cache
            await _cache.RemoveAsync(cacheKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update profile for user {UserId} in guild {GuildId}",
                profile.Id,
                profile.GuildProfiles);
            throw;
        }
    }

    /// <summary>
    /// Updates a user's XP and calculates if they leveled up.
    /// </summary>
    /// <param name="userId">Discord user ID</param>
    /// <param name="guildId">Discord guild ID</param>
    /// <param name="xpToAdd">Amount of XP to add</param>
    /// <returns>Tuple containing (new total XP, new level, whether user leveled up)</returns>
    public async Task<(int NewXp, int NewLevel, bool DidLevelUp)> UpdateUserXpAsync(
        ulong userId, 
        ulong guildId, 
        int xpToAdd,
        CancellationToken cancellationToken = default)
    {
        string userIdStr = userId.ToString();
        string guildIdStr = guildId.ToString();
        string cacheKey = $"profile:{userIdStr}:{guildIdStr}";

        try
        {
            // Get table lock
            SemaphoreSlim tableLock = GetTableLock("guild_members");
            await tableLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Get user profile
                var profile = await GetOrCreateUserProfileAsync(userId, guildId, cancellationToken);
                var guildProfile = profile.GetOrCreateGuildProfile(guildIdStr);
                
                // Store old level
                int oldLevel = guildProfile.Level;
                
                // Add XP
                guildProfile.Xp += xpToAdd;
                
                // Calculate new level
                int newLevel = CalculateLevel(guildProfile.Xp);
                guildProfile.Level = newLevel;
                
                // Update last active
                guildProfile.LastActive = DateTime.UtcNow;
                profile.LastActive = DateTime.UtcNow;
                
                // Update profile in database
                await ExecuteWithRetryAsync(
                    async () => await _supabase
                        .From<UserProfile>()
                        .Where(p => p.Id == userIdStr)
                        .Update(profile),
                    "UpdateUserXp",
                    cancellationToken)
                    .ConfigureAwait(false);
                
                // Update cache
                await _cache.SetAsync(
                    cacheKey,
                    profile,
                    TimeSpan.FromMinutes(5))
                    .ConfigureAwait(false);
                
                return (guildProfile.Xp, guildProfile.Level, guildProfile.Level > oldLevel);
            }
            finally
            {
                tableLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update XP for user {UserId} in guild {GuildId}",
                userId,
                guildId);
            throw;
        }
    }

    /// <summary>
    /// Calculates the level based on total XP.
    /// </summary>
    private static int CalculateLevel(int xp)
    {
        // Level formula: level = sqrt(xp / 100)
        return (int)Math.Floor(Math.Sqrt(xp / 100.0));
    }

    #endregion

    #region Guild Settings Operations

    /// <summary>
    /// Gets or creates guild settings.
    /// </summary>
    public async Task<GuildSettings> GetOrCreateGuildSettingsAsync(
        ulong guildId,
        CancellationToken cancellationToken = default)
    {
        string guildIdStr = guildId.ToString();
        string cacheKey = $"settings:{guildIdStr}";

        try
        {
            // Try cache first
            if (await _cache.GetAsync<GuildSettings>(cacheKey).ConfigureAwait(false) is GuildSettings cached)
            {
                return cached;
            }

            // Get table lock
            SemaphoreSlim tableLock = GetTableLock("guild_settings");
            await tableLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Check database
                var settings = await ExecuteWithRetryAsync(
                    async () => await _supabase
                        .From<GuildSettings>()
                        .Where(s => s.GuildId == guildIdStr)
                        .Single(),
                    "GetGuildSettings",
                    cancellationToken)
                    .ConfigureAwait(false);

                if (settings is null)
                {
                    // Create new settings
                    settings = new GuildSettings
                    {
                        GuildId = guildIdStr,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };

                    await ExecuteWithRetryAsync(
                        async () => await _supabase
                            .From<GuildSettings>()
                            .Insert(settings),
                        "CreateGuildSettings",
                        cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation(
                        "Created new settings for guild {GuildId}",
                        guildId);
                }

                // Cache result
                await _cache.SetAsync(
                    cacheKey,
                    settings,
                    TimeSpan.FromMinutes(5))
                    .ConfigureAwait(false);

                return settings;
            }
            finally
            {
                tableLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get/create settings for guild {GuildId}",
                guildId);
            throw;
        }
    }

    /// <summary>
    /// Updates guild settings.
    /// </summary>
    public async Task UpdateGuildSettingsAsync(
        GuildSettings settings,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"settings:{settings.GuildId}";

        try
        {
            settings.UpdatedAt = DateTime.UtcNow;

            await ExecuteWithRetryAsync(
                async () => await _supabase
                    .From<GuildSettings>()
                    .Where(s => s.GuildId == settings.GuildId)
                    .Update(settings),
                "UpdateGuildSettings",
                cancellationToken)
                .ConfigureAwait(false);

            // Clear cache
            await _cache.RemoveAsync(cacheKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update settings for guild {GuildId}",
                settings.GuildId);
            throw;
        }
    }

    #endregion

    #region Warning Operations

    /// <summary>
    /// Gets all active warnings for a user in a guild.
    /// </summary>
    public async Task<IReadOnlyList<Warning>> GetActiveWarningsAsync(
        ulong userId,
        ulong guildId,
        CancellationToken cancellationToken = default)
    {
        string userIdStr = userId.ToString();
        string guildIdStr = guildId.ToString();

        try
        {
            var warnings = await ExecuteWithRetryAsync(
                async () => await _supabase
                    .From<Warning>()
                    .Where(w => 
                        w.UserId == userIdStr && 
                        w.GuildId == guildIdStr)
                    .Get(),
                "GetWarnings",
                cancellationToken)
                .ConfigureAwait(false);

            return warnings.Models
                .Where(w => !w.IsExpired)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get warnings for user {UserId} in guild {GuildId}",
                userId,
                guildId);
            throw;
        }
    }

    /// <summary>
    /// Adds a warning for a user.
    /// </summary>
    public async Task AddWarningAsync(
        Warning warning,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteWithRetryAsync(
                async () => await _supabase
                    .From<Warning>()
                    .Insert(warning),
                "AddWarning",
                cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Added warning for user {UserId} in guild {GuildId}",
                warning.UserId,
                warning.GuildId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add warning for user {UserId} in guild {GuildId}",
                warning.UserId,
                warning.GuildId);
            throw;
        }
    }

    #endregion

    #region Metrics

    /// <summary>
    /// Saves bot metrics to the database
    /// </summary>
    public async Task SaveMetricsAsync(BotMetrics metrics)
    {
        try
        {
            await ExecuteWithRetryAsync(async () =>
            {
                return await _supabase
                    .From<BotMetrics>()
                    .Insert(metrics);
            }, "SaveMetrics");
            
            _logger.LogInformation("Saved metrics from {Timestamp}", metrics.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save metrics");
        }
    }

    /// <summary>
    /// Gets all active voice sessions
    /// </summary>
    public async Task<List<object>> GetActiveVoiceSessionsAsync()
    {
        try
        {
            // This is a stub implementation since the voice session tracking isn't implemented yet
            // Return an empty list for now
            return new List<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active voice sessions");
            return new List<object>();
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        _semaphore.Dispose();
        foreach (var tableLock in _tableLocks.Values)
        {
            tableLock.Dispose();
        }
        await ValueTask.CompletedTask;
    }
}