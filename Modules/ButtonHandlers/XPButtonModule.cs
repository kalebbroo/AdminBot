using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using AdminBot.Services;
using AdminBot.Models;
using System.Collections.Generic;

namespace AdminBot.Modules.ButtonHandlers;

public class XPButtonModule : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly DatabaseService _database;
    private readonly ILogger<XPButtonModule> _logger;

    public XPButtonModule(DatabaseService database, ILogger<XPButtonModule> logger)
    {
        _database = database;
        _logger = logger;
    }

    [ComponentInteraction("leaderboard_prev_*")]
    public async Task HandlePreviousPageAsync()
    {
        var currentPage = int.Parse(Context.Interaction.Data.CustomId.Split('_').Last());
        if (currentPage <= 1) return;

        InteractionCallback callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        const int ItemsPerPage = 10;
        var guildId = Context.Guild.Id;
        var targetPage = Math.Max(1, currentPage - 1);

        // Get profiles for this page
        var profiles = await GetLeaderboardProfilesAsync(guildId, targetPage, ItemsPerPage);
        if (profiles == null || !profiles.Any())
        {
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ No profiles found.");
            return;
        }

        var fields = new List<EmbedFieldProperties>();
        var rank = (targetPage - 1) * ItemsPerPage + 1;

        foreach (var profile in profiles)
        {
            var medal = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{rank}"
            };

            fields.Add(new EmbedFieldProperties
            {
                Name = $"{medal} {profile.GuildId}",
                Value = $"Level {profile.Level} | {profile.Xp:N0} XP\n" +
                       $"Messages: {profile.MessagesSent:N0} | Voice: {profile.VoiceMinutes / 60}h"
            });

            rank++;
        }

        var actionRow = new ActionRowProperties()
            .WithButtons(new[]
            {
                new ButtonProperties(
                    $"leaderboard_prev_{targetPage}",
                    "⬅️ Previous",
                    ButtonStyle.Secondary)
                {
                    Disabled = targetPage <= 1
                },
                new ButtonProperties(
                    $"leaderboard_next_{targetPage}",
                    "Next ➡️",
                    ButtonStyle.Secondary)
            });

        await Context.Interaction.ModifyResponseAsync(response =>
        {
            response.Embeds =
            [
                new EmbedProperties
                {
                    Title = $"🏆 {Context.Guild.Name} Leaderboard",
                    Fields = fields.ToArray(),
                    Footer = new EmbedFooterProperties { Text = $"Page {targetPage}" }
                }
            ];
            response.Components = [actionRow];
        });
    }

    [ComponentInteraction("leaderboard_next_*")]
    public async Task HandleNextPageAsync()
    {
        var currentPage = int.Parse(Context.Interaction.Data.CustomId.Split('_').Last());

        InteractionCallback callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        const int ItemsPerPage = 10;
        var guildId = Context.Guild.Id;
        var targetPage = currentPage + 1;

        // Get profiles for this page
        var profiles = await GetLeaderboardProfilesAsync(guildId, targetPage, ItemsPerPage);
        if (profiles == null || !profiles.Any())
        {
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ No more profiles to show.");
            return;
        }

        var fields = new List<EmbedFieldProperties>();
        var rank = (targetPage - 1) * ItemsPerPage + 1;

        foreach (var profile in profiles)
        {
            var medal = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{rank}"
            };

            fields.Add(new EmbedFieldProperties
            {
                Name = $"{medal} {profile.GuildId}",
                Value = $"Level {profile.Level} | {profile.Xp:N0} XP\n" +
                       $"Messages: {profile.MessagesSent:N0} | Voice: {profile.VoiceMinutes / 60}h"
            });

            rank++;
        }

        var actionRow = new ActionRowProperties()
            .WithButtons(new[]
            {
                new ButtonProperties(
                    $"leaderboard_prev_{targetPage}",
                    "⬅️ Previous",
                    ButtonStyle.Secondary),
                new ButtonProperties(
                    $"leaderboard_next_{targetPage}",
                    "Next ➡️",
                    ButtonStyle.Secondary)
            });

        await Context.Interaction.ModifyResponseAsync(response =>
        {
            response.Embeds =
            [
                new EmbedProperties
                {
                    Title = $"🏆 {Context.Guild.Name} Leaderboard",
                    Fields = fields.ToArray(),
                    Footer = new EmbedFooterProperties { Text = $"Page {targetPage}" }
                }
            ];
            response.Components = [actionRow];
        });
    }

    private async Task<List<GuildProfile>> GetLeaderboardProfilesAsync(ulong guildId, int page, int itemsPerPage)
    {
        try
        {
            var skip = (page - 1) * itemsPerPage;
            var profiles = new List<GuildProfile>();
            var guildIdStr = guildId.ToString();

            // Get profiles ordered by XP
            for (int i = 0; i < itemsPerPage; i++)
            {
                var userProfile = await _database.GetOrCreateUserProfileAsync((ulong)i, guildId);
                if (userProfile != null)
                {
                    var guildProfile = userProfile.GetGuildProfile(guildIdStr);
                    if (guildProfile != null)
                    {
                        profiles.Add(guildProfile);
                    }
                }
            }

            return profiles.OrderByDescending(p => p.Xp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard profiles for guild {GuildId}", guildId);
            return new List<GuildProfile>();
        }
    }
}
