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
public class UserModalModule(ILogger<AdminModalModule> logger, DatabaseService database,
    ICacheService cache) : ComponentInteractionModule<ModalInteractionContext>
{
    private readonly ILogger<AdminModalModule> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;

    [ComponentInteraction("profile_customize")]
    public async Task HandleProfileCustomizeModalAsync(string profileColor, string profileBanner)
    {
        var callback = InteractionCallback.DeferredMessage(MessageFlags.Ephemeral);
        await RespondAsync(callback);

        try
        {
            var userId = Context.User.Id;
            var guildId = Context.Guild.Id;
            var profile = await _database.GetOrCreateUserProfileAsync(userId, guildId);

            bool updated = false;

            if (updated)
            {
                await _database.UpdateUserProfileAsync(profile);
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "✅ Profile customization updated!");
            }
            else
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "Please provide a valid hex color code starting with # and/or a valid banner URL.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile customization for user {UserId}", Context.User.Id);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ Failed to update profile customization");
        }
    }
}
