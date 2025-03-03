using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using AdminBot.Services;
using AdminBot.Models;
using System.ComponentModel;

namespace AdminBot.Modules;

/// <summary>
/// Provides user-level commands for profile management and interaction.
/// </summary>
public class UserCommands(ILogger<UserCommands> logger, DatabaseService database, ICacheService cache) : ApplicationCommandModule<SlashCommandContext>
{
    private readonly ILogger<UserCommands> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;

    [SlashCommand("profile", "View your or another user's profile")]
    public async Task ProfileAsync(
        [SlashCommandParameter(Description = "User to view profile of (optional)")]
        User? user = null)
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            user ??= Context.User;
            var userId = user.Id;
            var guildId = Context.Guild.Id;

            var profile = await _database.GetOrCreateUserProfileAsync(userId, guildId);
            var warningCount = await _database.GetActiveWarningsAsync(userId, guildId);

            var embed = new EmbedProperties
            {
                Title = $"{user.Username}'s Profile",
                Description = $"Member since {profile.CreatedAt:MMM dd, yyyy}",
                Color = new(0x007bff),
                Thumbnail = user.GetAvatarUrl().ToString(),
            };

            await Context.Interaction.ModifyResponseAsync(response =>
            {
                response.Embeds = [embed];
                response.Components = 
                [
                    new ActionRowProperties()
                        .AddButtons(
                            new ButtonProperties("profile_stats:" + userId, "View Stats", new("📊"), ButtonStyle.Primary),
                            new ButtonProperties("profile_customize", "Customize", new("🎨"), ButtonStyle.Secondary)
                        )
                ];
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing profile for user {UserId}", user?.Id);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ Failed to load profile");
        }
    }

    [SlashCommand("stats", "View detailed statistics")]
    public async Task StatsAsync(
        [SlashCommandParameter(Description = "User to view stats for (optional)")]
        User? user = null)
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            user ??= Context.User;
            var userId = user.Id;
            var guildId = Context.Guild.Id;
            var profile = await _database.GetOrCreateUserProfileAsync(userId, guildId);

            var embed = new EmbedProperties
            {
                Title = $"{user.Username}'s Statistics",
                Description = "Current Statistics",
                Color = new(0x007bff),
                Timestamp = DateTimeOffset.UtcNow
            };

            await Context.Interaction.ModifyResponseAsync(response =>
                response.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing stats for user {UserId}", user?.Id);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ Failed to load statistics");
        }
    }
}