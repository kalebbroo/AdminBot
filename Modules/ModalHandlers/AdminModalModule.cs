using Microsoft.Extensions.Logging;
using AdminBot.Services;
using NetCord;
using NetCord.Services.ComponentInteractions;
using NetCord.Rest;
using AdminBot.Models;

namespace AdminBot.Modules.ModalHandlers;

/// <summary>
/// Handles modal interactions for admin-related functionality.
/// </summary>
public class AdminModalModule(
    ILogger<AdminModalModule> logger,
    DatabaseService database,
    ICacheService cache) : ComponentInteractionModule<ModalInteractionContext>
{
    private readonly ILogger<AdminModalModule> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;

    /// <summary>
    /// Handles the server setup modal submission.
    /// </summary>
    [ComponentInteraction("server_setup")]
    public async Task HandleServerSetupAsync()
    {
        try
        {
            // First defer the response since we'll be doing database operations
            var callback = InteractionCallback.DeferredMessage(MessageFlags.Ephemeral);
            await RespondAsync(callback);

            // Get values from the modal components
            var textInputs = Context.Components.OfType<TextInput>();
            var welcomeChannel = textInputs.First(c => c.CustomId == "welcome_channel").Value;
            var rules = textInputs.First(c => c.CustomId == "rules").Value;

            // Update guild settings
            var guildId = Context.Guild.Id;
            var settings = await _database.GetOrCreateGuildSettingsAsync(guildId);
            
            if (ulong.TryParse(welcomeChannel, out var channelId))
            {
                settings.Channels.WelcomeChannelId = channelId.ToString();
            }
            settings.Rules = rules;
            settings.UpdatedAt = DateTime.UtcNow;

            await _database.UpdateGuildSettingsAsync(settings);
            await _cache.RemoveAsync($"guild_settings:{guildId}");

            // Send success message as a follow-up
            var successMessage = new InteractionMessageProperties()
                .WithContent("✅ Server settings updated successfully!")
                .WithFlags(MessageFlags.Ephemeral);
            await Context.Interaction.SendFollowupMessageAsync(successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling server setup modal for guild {GuildId}", Context.Guild.Id);
            var errorMessage = new InteractionMessageProperties()
                .WithContent("❌ Failed to update server settings")
                .WithFlags(MessageFlags.Ephemeral);
            await Context.Interaction.SendFollowupMessageAsync(errorMessage);
        }
    }
}
