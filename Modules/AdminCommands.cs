using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using AdminBot.Services;
using AdminBot.Utils.Helpers;
using AdminBot.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AdminBot.Modules;

/// <summary>
/// Provides administrative commands for server management and moderation.
/// Handles all admin-level interactions following NetCord's command pattern.
/// </summary>
public enum WarningSeverity
{
    [SlashCommandChoice("Low")]
    Low = 1,
    [SlashCommandChoice("Medium")]
    Medium = 2,
    [SlashCommandChoice("High")]
    High = 3
}

public enum XpEventType
{
    [SlashCommandChoice("Double XP")]
    DoubleXp,
    [SlashCommandChoice("Triple XP")]
    TripleXp,
    [SlashCommandChoice("Custom Multiplier")]
    Custom
}

public enum ConfigAction
{
    [SlashCommandChoice("View")]
    View,
    [SlashCommandChoice("Reset")]
    Reset
}

public class AdminCommands(ILogger<AdminCommands> logger, DatabaseService database,
    ICacheService cache) : ApplicationCommandModule<SlashCommandContext>
{
    private readonly ILogger<AdminCommands> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;

    [SlashCommand("setup", "Configure server settings")]
    public async Task SetupServerAsync()
    {
        try
        {
            var modal = new ModalProperties("server_setup", "Server Setup")
                .AddComponents(
                    new TextInputProperties("welcome_channel", TextInputStyle.Short, "Welcome Channel")
                        .WithPlaceholder("Channel ID for welcome messages")
                        .WithMaxLength(20)
                        .WithRequired(true),
                    new TextInputProperties("rules", TextInputStyle.Paragraph, "Rules")
                        .WithPlaceholder("Server rules")
                        .WithMaxLength(1000)
                        .WithRequired(true)
                );

            var callback = InteractionCallback.Modal(modal);
            await RespondAsync(callback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing server setup modal");
            await Context.Interaction.SendFollowupMessageAsync("❌ An error occurred while showing the setup modal.");
        }
    }

    [SlashCommand("warn", "Issue a warning to a user")]
    public async Task WarnUserAsync(
        [SlashCommandParameter(Description = "User to warn")]
        User user,
        [SlashCommandParameter(Description = "Reason for warning")]
        string reason,
        [SlashCommandParameter(Description = "Warning severity")]
        WarningSeverity severity = WarningSeverity.Low)
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);
        try
        {
            // Get or create the user's profile first
            var profile = await _database.GetOrCreateUserProfileAsync(user.Id, Context.Guild.Id);
            if (profile == null)
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ Could not create user profile.");
                return;
            }
            // Update the profile with the warning
            profile.Warnings++;
            await _database.UpdateUserProfileAsync(profile);
            var embed = new EmbedProperties
            {
                Title = "⚠️ Warning Issued",
                Description = reason,
                Color = GetSeverityColor((int)severity),
                Timestamp = DateTimeOffset.UtcNow
            };
            // Send warning to channel
            await Context.Interaction.ModifyResponseAsync(response => 
                response.Embeds = [embed]);
            // Try to DM the user
            try
            {
                var dmEmbed = new EmbedProperties
                {
                    Title = "⚠️ Warning Received",
                    Description = $"You have received a warning in {Context.Guild.Name}",
                    Fields = 
                    [
                        new EmbedFieldProperties { Name = "Reason", Value = reason, Inline = true },
                        new EmbedFieldProperties { Name = "Severity", Value = GetSeverityText((int)severity), Inline = true }
                    ],
                    Color = GetSeverityColor((int)severity),
                    Timestamp = DateTimeOffset.UtcNow
                };

                var dmChannel = await user.GetDMChannelAsync();
                await dmChannel.SendMessageAsync(new MessageProperties { Embeds = [dmEmbed] });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send warning DM to user {UserId}", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error issuing warning");
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while issuing the warning.");
        }
    }

    [SlashCommand("xp-event", "Create an XP event")]
    public async Task CreateXpEventAsync(
        [SlashCommandParameter(Description = "Event type")]
        XpEventType type,
        [SlashCommandParameter(Description = "Duration in hours")]
        [Range(1, 168)]
        int duration,
        [SlashCommandParameter(Description = "Custom multiplier (if type is custom)")]
        [Range(1.1, 5.0)]
        double? multiplier = null)
    {
        InteractionCallback callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            decimal eventMultiplier = type switch
            {
                XpEventType.DoubleXp => 2.0m,
                XpEventType.TripleXp => 3.0m,
                _ => (decimal)(multiplier ?? 1.0)
            };
            ulong guildId = Context.Guild.Id;
            // Get or create guild profile to store event
            UserProfile? userProfile = await _database.GetOrCreateUserProfileAsync(guildId, Context.Guild.Id);
            GuildProfile guildProfile = userProfile.GetGuildProfile(guildId.ToString());
            if (guildProfile == null)
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ Could not create guild profile.");
                return;
            }

            // Update profile with event details
            //guildProfile.Xp = eventMultiplier;
            //guildProfile.XpBoostEndTime = DateTime.UtcNow.AddHours(duration);
            //await _database.UpdateUserProfileAsync(guildProfile);

            // Clear cache
            await _cache.RemoveAsync($"active_events:{Context.Guild.Id}");

            var embed = new EmbedProperties
            {
                Title = "🎉 XP Event Created",
                Description = $"A new XP event has been created for {Context.Guild.Name}!",
                Fields =
                [
                    new EmbedFieldProperties { Name = "Type", Value = GetEventTypeText(type), Inline = true },
                    new EmbedFieldProperties { Name = "Multiplier", Value = $"{eventMultiplier}x", Inline = true },
                    new EmbedFieldProperties { Name = "Duration", Value = $"{duration} hours", Inline = true },
                    new EmbedFieldProperties { Name = "Start Time", Value = $"<t:{((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds()}:F>", Inline = true },
                    new EmbedFieldProperties { Name = "End Time", Value = $"<t:{((DateTimeOffset)DateTime.UtcNow.AddHours(duration)).ToUnixTimeSeconds()}:F>", Inline = true }
                ],
                Color = new(0xFFA500),
                Timestamp = DateTimeOffset.UtcNow
            };

            await Context.Interaction.ModifyResponseAsync(response =>
                response.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating XP event");
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while creating the XP event.");
        }
    }

    [SlashCommand("config", "View or modify server configuration")]
    public async Task ConfigureServerAsync(
        [SlashCommandParameter(Description = "Configuration action")]
        ConfigAction action = ConfigAction.View)
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            ulong guildId = Context.Guild.Id;
            var profile = await _database.GetOrCreateUserProfileAsync(guildId, guildId);
            if (profile == null)
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ Could not get guild profile.");
                return;
            }
            if (action == ConfigAction.Reset)
            {
                // Reset profile settings to default
                //profile.XpMultiplier = 1.0m;
                //profile.XpBoostEndTime = null;
                //profile.WelcomeChannelId = null;
                await _database.UpdateUserProfileAsync(profile);
                await _cache.RemoveAsync($"guild_settings:{guildId}");
            }
            var embed = new EmbedProperties
            {
                Title = "Server Configuration",
                Color = new(0xFFA500),
                Timestamp = DateTimeOffset.UtcNow
            };
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring server");
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while configuring the server.");
        }
    }

    private static Color GetSeverityColor(int severity) => severity switch
    {
        1 => new Color(255, 255, 0),  // Yellow for Low
        2 => new Color(255, 165, 0),  // Orange for Medium
        3 => new Color(255, 0, 0),    // Red for High
        _ => new Color(128, 128, 128)  // Gray for unknown
    };

    private static string GetSeverityText(int severity) => severity switch
    {
        1 => "Low",
        2 => "Medium",
        3 => "High",
        _ => "Unknown"
    };

    private static string GetEventTypeText(XpEventType type) => type switch
    {
        XpEventType.DoubleXp => "Double XP",
        XpEventType.TripleXp => "Triple XP",
        XpEventType.Custom => "Custom Multiplier",
        _ => "Unknown"
    };
}