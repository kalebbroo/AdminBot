using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using AdminBot.Services;

namespace AdminBot.Events;

/// <summary>
/// Handles message reaction events following NetCord's event handling pattern.
/// Manages reaction roles, XP awards, and reaction tracking.
/// </summary>
[GatewayEvent(nameof(GatewayClient.MessageReactionAdd))]
public class ReactionHandler(ILogger<ReactionHandler> logger, DatabaseService database, 
    ICacheService cache) : IGatewayEventHandler<MessageReactionAddEventArgs>
{
    private readonly ILogger<ReactionHandler> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;
    private readonly Random _random = new Random();

    /// <summary>
    /// Handles reaction events according to NetCord's event pattern.
    /// </summary>
    public async ValueTask HandleAsync(MessageReactionAddEventArgs args)
    {
        try
        {
            // Get guild ID if reaction is in a guild
            var guildId = args.GuildId;
            if (guildId == null)
            {
                return; // DM or unknown channel type
            }

            var userId = args.UserId;

            // Process reaction for XP
            await ProcessReactionXp(userId.ToString(), guildId.ToString());

            _logger.LogInformation(
                "Processed reaction from user {UserId} in guild {GuildId}",
                userId,
                guildId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing reaction from user {UserId}",
                args.UserId);
        }
    }

    private async Task ProcessReactionXp(string userId, string guildId)
    {
        // Check cooldown using cache
        var cooldownKey = $"reaction_xp_cooldown:{userId}:{guildId}";
        if (await _cache.ExistsAsync(cooldownKey))
        {
            return;
        }
        // TODO: Implement XP processing

    }

    private async Task UpdateReactionStats(ulong userId, ulong guildId)
    {
        var profile = await _database.GetOrCreateUserProfileAsync(userId, guildId);
        profile.LastActive = DateTime.UtcNow;

        await _database.UpdateUserProfileAsync(profile);
    }
}