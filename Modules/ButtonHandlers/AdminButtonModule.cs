using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using AdminBot.Services;
using AdminBot.Models;

namespace AdminBot.Modules.ButtonHandlers;

public class AdminButtonModule(ILogger<AdminButtonModule> logger, DatabaseService database,
    ICacheService cache) : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ILogger<AdminButtonModule> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;

    [ComponentInteraction("warn_confirm_*")]
    public async Task HandleWarnConfirmationAsync()
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            var warningId = Context.Interaction.Data.CustomId.Split('_').Last();
            Warning warning = new Warning
            {
                GuildId = Context.Guild.Id.ToString(),
                // TODO: Add full warning
            };

            await _database.AddWarningAsync(warning);

            var embed = new EmbedProperties
            {
                Title = "✅ Warning Confirmed",
                Description = "The warning has been confirmed and recorded.",
            };

            await Context.Interaction.ModifyResponseAsync(response =>
            {
                response.Embeds = [embed];
                response.Components = [];
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling warning confirmation");
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while confirming the warning.");
        }
    }

    [ComponentInteraction("warn_cancel_*")]
    public async Task HandleWarnCancellationAsync()
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            var warningId = Context.Interaction.Data.CustomId.Split('_').Last();
            var warning = await _database.GetActiveWarningsAsync(Context.Interaction.User.Id, Context.Guild.Id);
            
            if (warning == null)
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ Warning not found.");
                return;
            }

            //await _database.DeleteWarningAsync(warningId);

            var embed = new EmbedProperties
            {
                Title = "❌ Warning Cancelled",
                Description = "The warning has been cancelled and removed.",
            };

            await Context.Interaction.ModifyResponseAsync(response =>
            {
                response.Embeds = [embed];
                response.Components = [];
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling warning cancellation");
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while cancelling the warning.");
        }
    }
}
