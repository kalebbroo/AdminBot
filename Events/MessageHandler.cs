using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using AdminBot.Services;

namespace AdminBot.Events;

/// <summary>
/// Handles message creation events for XP calculation and command processing.
/// Follows NetCord's event handler pattern as specified in the documentation.
/// </summary>
[GatewayEvent(nameof(GatewayClient.MessageCreate))]
public class MessageCreateHandler : IGatewayEventHandler<Message>
{
    private readonly ILogger<MessageCreateHandler> _logger;
    private readonly DatabaseService _database;
    private readonly ICacheService _cache;
    private readonly Random _random;

    public MessageCreateHandler(
        ILogger<MessageCreateHandler> logger,
        DatabaseService database,
        ICacheService cache)
    {
        _logger = logger;
        _database = database;
        _cache = cache;
        _random = new Random();
    }

    /// <summary>
    /// Handles incoming messages according to NetCord's event handling pattern.
    /// </summary>
    public async ValueTask HandleAsync(Message message)
    {
        try
        {
            // Ignore bot messages and system messages
            if (message.Author.IsBot || message.Type != MessageType.Default)
            {
                return;
            }

            // Get the guild ID if message is from a guild
            var guildId = message.GuildId;
            if (guildId == null)
            {
                return; // DM or unknown channel type
            }

            var userId = message.Author.Id;

            // Process message for XP
            await ProcessMessageXp(userId, guildId.Value, message);

            // Update message statistics
            await UpdateMessageStats(userId, guildId.Value);

            _logger.LogInformation(
                "Processed message from user {UserId} in guild {GuildId}",
                userId,
                guildId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing message from user {UserId}",
                message.Author.Id);
        }
    }

    private async Task ProcessMessageXp(ulong userId, ulong guildId, Message message)
    {
        // TODO: Implement XP calculation and update user profile
    }

    private async Task UpdateMessageStats(ulong userId, ulong guildId)
    {
        var profile = await _database.GetOrCreateUserProfileAsync(userId, guildId);
        profile.LastActive = DateTime.UtcNow;

        // Update stats in database
        await _database.UpdateUserProfileAsync(profile);
    }

    private int CalculateMessageXp(Message message, int baseXp)
    {
        // Base calculation
        var xp = baseXp;

        // Bonus for longer messages
        if (message.Content.Length > 100)
        {
            xp += 2;
        }

        // Random variation (±20%)
        var variation = _random.Next(-20, 21) / 100.0;
        xp = (int)(xp * (1 + variation));

        return Math.Max(1, xp); // Ensure at least 1 XP
    }
}