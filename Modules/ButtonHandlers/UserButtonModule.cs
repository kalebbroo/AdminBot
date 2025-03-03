using NetCord;
using NetCord.Services;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using AdminBot.Services;
using AdminBot.Models;
using Microsoft.Extensions.Logging;
using NetCord.Services.ComponentInteractions;

namespace AdminBot.Modules.ButtonHandlers;

public class UserButtonModule : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly DatabaseService _database;
    private readonly ICacheService _cache;
    private readonly ILogger<UserButtonModule> _logger;

    public UserButtonModule(
        DatabaseService database,
        ICacheService cache,
        ILogger<UserButtonModule> logger)
    {
        _database = database;
        _cache = cache;
        _logger = logger;
    }

    [ComponentInteraction("stats_*")]
    public async Task HandleStatsButtonAsync(string userId)
    {
        try
        {
            var callback = InteractionCallback.DeferredMessage();
            await RespondAsync(callback);

            ulong userIdValue = ulong.Parse(userId);
            User user = await Context.Guild.GetUserAsync(userIdValue);
            ulong guildId = Context.Guild.Id;

            var profile = await _database.GetOrCreateUserProfileAsync(userIdValue, guildId);

            var embed = new EmbedProperties
            {
                Title = $"{user.Username}'s Statistics",
                Description = "Activity over the last 7 days",
                Fields =
                [
                    new EmbedFieldProperties
                    {
                        Name = "Message Activity",
                        Value = $"Total Messages:",
                        Inline = true
                    },
                ],
            };

            await Context.Interaction.ModifyResponseAsync(response =>
                response.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling stats button for user {UserId}", userId);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ Failed to load user statistics");
        }
    }

    [ComponentInteraction("profile_stats:*")]
    public async Task HandleProfileStatsButtonAsync(string userId)
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            var targetUser = await Context.Guild.GetUserAsync(ulong.Parse(userId));
            var guildId = Context.Guild.Id;
            var profile = await _database.GetOrCreateUserProfileAsync(ulong.Parse(userId), guildId);

            var embed = new EmbedProperties
            {
                Title = $"{targetUser.Username}'s Statistics",
                Description = "Current Statistics",
                Color = new(0x007bff),
                Thumbnail = targetUser.GetAvatarUrl()?.ToString() ?? targetUser.DefaultAvatarUrl.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };

            await Context.Interaction.ModifyResponseAsync(response =>
                response.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing stats for user {UserId}", userId);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ Failed to load statistics");
        }
    }

    [ComponentInteraction("profile_customize")]
    public async Task HandleProfileCustomizeButtonAsync()
    {
        try
        {
            var modal = new ModalProperties("profile_customize", "Customize Profile")
                .AddComponents(
                    new TextInputProperties("profile_color", TextInputStyle.Short, "Profile Color")
                        .WithPlaceholder("Enter a hex color code (e.g., #FF0000)")
                        .WithMaxLength(7)
                        .WithRequired(false),
                    new TextInputProperties("profile_banner", TextInputStyle.Short, "Profile Banner URL")
                        .WithPlaceholder("Enter a URL for your profile banner")
                        .WithMaxLength(200)
                        .WithRequired(false)
                );

            var modalCallback = InteractionCallback.Modal(modal);
            await RespondAsync(modalCallback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing customize modal");
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ Failed to show customization options");
        }
    }
}
