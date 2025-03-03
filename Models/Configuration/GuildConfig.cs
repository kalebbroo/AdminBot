using System.Text.Json.Serialization;
using NetCord;

namespace AdminBot.Models.Configuration;

/// <summary>
/// Represents guild-specific configuration settings.
/// Handles per-server settings and customization options.
/// </summary>
public sealed class GuildConfig : IConfiguration, IFeatureConfiguration, IChannelConfiguration
{
    [JsonPropertyName("guild_id")]
    public string GuildId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = "!";

    [JsonPropertyName("features")]
    public Dictionary<string, bool> Features { get; set; } = new();

    [JsonPropertyName("channel_ids")]
    public Dictionary<string, string> ChannelIds { get; set; } = new();

    [JsonPropertyName("role_ids")]
    public Dictionary<string, string> RoleIds { get; set; } = new();

    [JsonPropertyName("xp_settings")]
    public XpSettings XPSettings { get; set; } = new();

    [JsonPropertyName("moderation_settings")]
    public ModerationSettings Moderation { get; set; } = new();

    [JsonPropertyName("welcome_settings")]
    public WelcomeSettings Welcome { get; set; } = new();

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// XP and leveling configuration.
    /// </summary>
    public class XpSettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("message_xp")]
        public int MessageXp { get; set; } = 5;

        [JsonPropertyName("voice_xp_per_minute")]
        public int VoiceXpPerMinute { get; set; } = 2;

        [JsonPropertyName("cooldown_seconds")]
        public int CooldownSeconds { get; set; } = 60;

        [JsonPropertyName("level_roles")]
        public Dictionary<int, string> LevelRoles { get; set; } = new();

        [JsonPropertyName("level_multipliers")]
        public Dictionary<int, float> LevelMultipliers { get; set; } = new();
    }

    /// <summary>
    /// Moderation settings configuration.
    /// </summary>
    public sealed class ModerationSettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("warn_expire_days")]
        public int WarnExpireDays { get; set; } = 30;

        [JsonPropertyName("max_warnings")]
        public int MaxWarnings { get; set; } = 3;

        [JsonPropertyName("auto_actions")]
        public Dictionary<int, string> AutoActions { get; set; } = new();

        [JsonPropertyName("mod_roles")]
        public List<string> ModRoles { get; set; } = new();
    }

    /// <summary>
    /// Welcome message configuration.
    /// </summary>
    public sealed class WelcomeSettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("message")]
        public string Message { get; set; } = "Welcome {user} to {server}!";

        [JsonPropertyName("embed_color")]
        public Color EmbedColor { get; set; } = new(88, 101, 242);

        [JsonPropertyName("auto_roles")]
        public List<string> AutoRoles { get; set; } = new();
    }

    /// <summary>
    /// Creates default guild configuration.
    /// </summary>
    public static GuildConfig CreateDefault(string guildId, string name)
    {
        return new GuildConfig
        {
            GuildId = guildId,
            Name = name,
            Features = new Dictionary<string, bool>
            {
                ["xp"] = true,
                ["moderation"] = true,
                ["welcome"] = true,
                ["voice"] = true
            },
            ChannelIds = new Dictionary<string, string>
            {
                ["welcome"] = string.Empty,
                ["logs"] = string.Empty,
                ["announcements"] = string.Empty
            }
        };
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public bool Validate(out string[] errors)
    {
        List<string> errorList = new();

        if (string.IsNullOrEmpty(GuildId))
            errorList.Add("Guild ID is required");

        if (string.IsNullOrEmpty(Name))
            errorList.Add("Guild name is required");

        if (XPSettings.MessageXp < 0)
            errorList.Add("Message XP cannot be negative");

        if (XPSettings.VoiceXpPerMinute < 0)
            errorList.Add("Voice XP per minute cannot be negative");

        if (Moderation.WarnExpireDays < 0)
            errorList.Add("Warning expiry days cannot be negative");

        errors = errorList.ToArray();
        return errors.Length == 0;
    }

    /// <summary>
    /// Checks if a feature is enabled.
    /// </summary>
    public bool IsFeatureEnabled(string featureKey)
    {
        return Features.TryGetValue(featureKey, out bool enabled) && enabled;
    }

    /// <summary>
    /// Gets a channel ID for a specific purpose.
    /// </summary>
    public string? GetChannelId(string purpose)
    {
        return ChannelIds.TryGetValue(purpose, out string? channelId)
            ? channelId
            : null;
    }

    /// <summary>
    /// Gets a role ID for a specific purpose.
    /// </summary>
    public string? GetRoleId(string purpose)
    {
        return RoleIds.TryGetValue(purpose, out string? roleId)
            ? roleId
            : null;
    }
}