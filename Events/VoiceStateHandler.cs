using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using AdminBot.Services;

namespace AdminBot.Events;

/// <summary>
/// Handles voice state changes and voice activity tracking.
/// Implements NetCord's voice handling patterns for state management.
/// </summary>
[GatewayEvent(nameof(GatewayClient.VoiceStateUpdate))]
public class VoiceStateHandler(ILogger<VoiceStateHandler> logger, DatabaseService database,
    ICacheService cache) : IGatewayEventHandler<VoiceState>
{
    private readonly ILogger<VoiceStateHandler> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;
    private readonly Dictionary<ulong, DateTime> _voiceSessionStarts = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Handles voice state updates according to NetCord's event pattern.
    /// </summary>
    public async ValueTask HandleAsync(VoiceState args)
    {
        var userId = args.UserId;
        var guildId = args.GuildId;
        var channelId = args.ChannelId;

        try
        {
            await _semaphore.WaitAsync();
            try
            {
                if (channelId.HasValue)
                {
                    // User joined a voice channel
                    await HandleVoiceJoin(userId, guildId, channelId.Value);
                }
                else
                {
                    // User left a voice channel
                    await HandleVoiceLeave(userId, guildId);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling voice state change for user {UserId} in guild {GuildId}",
                userId,
                guildId);
        }
    }

    private async Task HandleVoiceJoin(ulong userId, ulong guildId, ulong channelId)
    {
        var sessionKey = userId;

        // Only start a new session if one isn't already active
        if (!_voiceSessionStarts.ContainsKey(sessionKey))
        {
            _voiceSessionStarts[sessionKey] = DateTime.UtcNow;

            // Cache the channel ID for the session
            await _cache.SetAsync(
                $"voice_channel:{sessionKey}",
                channelId,
                TimeSpan.FromHours(24));

            _logger.LogInformation(
                "User {UserId} joined voice channel {ChannelId} in guild {GuildId}",
                userId,
                channelId,
                guildId);
        }
    }

    private async Task HandleVoiceLeave(ulong userId, ulong guildId)
    {
        var sessionKey = userId;

        // Process voice time if there was an active session
        if (_voiceSessionStarts.TryGetValue(sessionKey, out var startTime))
        {
            var duration = DateTime.UtcNow - startTime;
            await ProcessVoiceTime(userId, guildId, duration);

            _voiceSessionStarts.Remove(sessionKey);

            // Remove the cached channel ID
            await _cache.RemoveAsync($"voice_channel:{sessionKey}");

            _logger.LogInformation(
                "User {UserId} left voice channel in guild {GuildId} after {Duration} minutes",
                userId,
                guildId,
                duration.TotalMinutes);
        }
    }

    private async Task ProcessVoiceTime(ulong userId, ulong guildId, TimeSpan duration)
    {
        try
        {
            // Get voice XP settings
            var settings = await _cache.GetOrCreateAsync(
                $"xp_settings:{guildId}",
                async () => (await _database.GetOrCreateGuildSettingsAsync(guildId)).XpSettings,
                TimeSpan.FromMinutes(5));

            // Calculate XP based on voice time
            var minutes = (int)Math.Floor(duration.TotalMinutes);
            if (minutes <= 0) return;

            var xpToAdd = minutes * settings.VoiceXpPerMinute;

            // Update XP and user stats
            var (newXp, newLevel, didLevelUp) = await _database.UpdateUserXpAsync(
                userId,
                guildId,
                xpToAdd);

            // Update voice minutes in profile
            var profile = await _database.GetOrCreateUserProfileAsync(userId, guildId);
            var guildProfile = profile.GetOrCreateGuildProfile(guildId.ToString());
            guildProfile.VoiceMinutes += minutes;
            guildProfile.LastActive = DateTime.UtcNow;
            await _database.UpdateUserProfileAsync(profile);

            if (didLevelUp)
            {
                _logger.LogInformation(
                    "User {UserId} leveled up to {Level} in guild {GuildId} from voice activity",
                    userId,
                    newLevel,
                    guildId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing voice time for user {UserId} in guild {GuildId}",
                userId,
                guildId);
        }
    }

    /// <summary>
    /// Cleans up resources when the handler is disposed.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }
}