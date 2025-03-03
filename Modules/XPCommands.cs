using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Services.ApplicationCommands;
using AdminBot.Services;
using AdminBot.Models;
using NetCord.Rest;

namespace AdminBot.Modules;

/// <summary>
/// Handles XP and leveling related commands.
/// Provides user progression tracking and leaderboard functionality.
/// </summary>
public class XPCommands(ILogger<XPCommands> logger, DatabaseService database, ICacheService cache) : ApplicationCommandModule<SlashCommandContext>
{
    private readonly ILogger<XPCommands> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;

    [SlashCommand("rank", "Check your or another user's rank")]
    public async Task RankAsync(
        [SlashCommandParameter(Description = "User to check rank for (optional)")] 
        User? user = null)
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            user ??= Context.User;
            var guildId = Context.Guild.Id;
            var guildIdStr = guildId.ToString();
            var userId = user.Id;

            var userProfile = await _database.GetOrCreateUserProfileAsync(userId, guildId);
            var guildProfile = userProfile.GetOrCreateGuildProfile(guildIdStr);
            
            // Calculate next level requirements
            //var nextLevelXp = CalculateRequiredXp(guildProfile.Level + 1);
            //var currentLevelXp = CalculateRequiredXp(guildProfile.Level);
            //var progressInLevel = guildProfile.Xp - currentLevelXp;
            //var totalForLevel = nextLevelXp - currentLevelXp;
            //var percentage = (double)progressInLevel / totalForLevel;

            // Create progress bar
            var progressBar = GenerateProgressBar(10, 20);

            var embed = new EmbedProperties
            {
                Title = $"{user.Username}'s Rank",
                Description = $"**Level:** {guildProfile.Level}\n**XP:** {guildProfile.Xp}\n**Voice Minutes:** {guildProfile.VoiceMinutes}",
                Thumbnail = user.GetAvatarUrl()?.ToString() ?? user.DefaultAvatarUrl.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };

            await Context.Interaction.ModifyResponseAsync(response =>
                response.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rank for user {UserId}", user?.Id);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while checking rank.");
        }
    }

    [SlashCommand("leaderboard", "View the server's XP leaderboard")]
    public async Task LeaderboardAsync(
        [SlashCommandParameter(Description = "Page number")] 
        int page = 1)
    {
        var callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            if (page < 1)
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ Page number must be greater than 0.");
                return;
            }

            const int ItemsPerPage = 10;
            var guildId = Context.Guild.Id;
            var guildIdStr = guildId.ToString();

            // Get all guild members for this page
            var guildProfiles = await _cache.GetOrCreateAsync(
                $"leaderboard:{guildId}:{page}",
                async () =>
                {
                    var memberProfiles = new List<(string UserId, GuildProfile Profile)>();
                    
                    // Fetch a sample user to demonstrate functionality
                    // This should be replaced with actual logic to fetch top users
                    var userProfile = await _database.GetOrCreateUserProfileAsync(Context.User.Id, guildId);
                    var guildProfile = userProfile.GetGuildProfile(guildIdStr);
                    if (guildProfile != null)
                    {
                        memberProfiles.Add((userProfile.Id, guildProfile));
                    }
                    
                    return memberProfiles;
                },
                TimeSpan.FromMinutes(5));

            var description = new System.Text.StringBuilder();
            for (int i = 0; i < guildProfiles.Count; i++)
            {
                var (userId, profile) = guildProfiles[i];
                var rank = (page - 1) * ItemsPerPage + i + 1;
                var medal = rank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => $"#{rank}"
                };

                description.AppendLine($"{medal} <@{userId}> • Level {profile.Level} • {profile.Xp} XP");
            }

            if (description.Length == 0)
            {
                description.AppendLine("*No data available yet*");
            }

            var embed = new EmbedProperties
            {
                Title = "🏆 XP Leaderboard",
                Description = description.ToString(),
                Footer = new EmbedFooterProperties 
                { 
                    Text = $"Page {page}" 
                },
                Color = new(0xffd700),
                Timestamp = DateTimeOffset.UtcNow
            };

            var components = new ActionRowProperties()
                .AddButtons(
                    new ButtonProperties("lb_prev:" + page, "Previous", new("⬅️"), ButtonStyle.Secondary)
                        .WithDisabled(page <= 1),
                    new ButtonProperties("lb_next:" + page, "Next", new("➡️"), ButtonStyle.Secondary)
                );

            await Context.Interaction.ModifyResponseAsync(response =>
            {
                response.Embeds = [embed];
                response.Components = [components];
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing leaderboard page {Page}", page);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while loading the leaderboard.");
        }
    }

    private static string GenerateProgressBar(double percentage, int length)
    {
        int filledLength = (int)(length * percentage);
        return $"[{new string('█', filledLength)}{new string('░', length - filledLength)}]";
    }

    private static Color GetLevelColor(int level) => level switch
    {
        >= 50 => new(0xdd2e44),  // Red
        >= 30 => new(0x9b59b6),  // Purple
        >= 20 => new(0x3498db),  // Blue
        >= 10 => new(0x2ecc71),  // Green
        _ => new(0x95a5a6)       // Gray
    };

    private static int CalculateRequiredXp(int level) =>
        level * level * 100;
}