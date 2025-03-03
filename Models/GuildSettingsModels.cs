using System.Text.Json.Serialization;
using System.Collections.Generic;
using Supabase.Postgrest.Models;

namespace AdminBot.Models;

/// <summary>
/// Represents server-specific settings and configurations.
/// Maps to the 'guild_settings' table in Supabase.
/// </summary>
public class GuildSettings : BaseModel
{
    /// <summary>
    /// Unique identifier for the settings (UUID in Supabase)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Discord guild ID
    /// </summary>
    [JsonPropertyName("guild_id")]
    public string GuildId { get; set; } = string.Empty;

    /// <summary>
    /// When these settings were created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time settings were updated
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Channel configurations
    /// </summary>
    [JsonPropertyName("channels")]
    public ChannelSettings Channels { get; set; } = new();

    /// <summary>
    /// XP and leveling configurations
    /// </summary>
    [JsonPropertyName("xp")]
    public XpSettings XpSettings { get; set; } = new();

    /// <summary>
    /// Moderation settings
    /// </summary>
    [JsonPropertyName("moderation")]
    public ModerationSettings ModerationSettings { get; set; } = new();

    /// <summary>
    /// Server rules text
    /// </summary>
    [JsonPropertyName("rules")]
    public string Rules { get; set; } = string.Empty;

    /// <summary>
    /// Welcome message configuration
    /// </summary>
    [JsonPropertyName("welcome")]
    public WelcomeSettings WelcomeSettings { get; set; } = new();

    /// <summary>
    /// Auto-role configurations
    /// </summary>
    [JsonPropertyName("auto_roles")]
    public List<AutoRole> AutoRoles { get; set; } = new();

    /// <summary>
    /// Reaction role configurations
    /// </summary>
    [JsonPropertyName("reaction_roles")]
    public List<ReactionRole> ReactionRoles { get; set; } = new();

    /// <summary>
    /// Custom commands for this guild
    /// </summary>
    [JsonPropertyName("custom_commands")]
    public List<CustomCommand> CustomCommands { get; set; } = new();

    /// <summary>
    /// Feature toggles and settings
    /// </summary>
    [JsonPropertyName("features")]
    public FeatureSettings Features { get; set; } = new();
}

/// <summary>
/// Represents channel-specific configurations
/// </summary>
public class ChannelSettings
{
    /// <summary>
    /// Welcome message channel ID
    /// </summary>
    [JsonPropertyName("welcome_channel")]
    public string? WelcomeChannelId { get; set; }

    /// <summary>
    /// Rules channel ID
    /// </summary>
    [JsonPropertyName("rules_channel")]
    public string? RulesChannelId { get; set; }

    /// <summary>
    /// Log channel ID for bot actions
    /// </summary>
    [JsonPropertyName("log_channel")]
    public string? LogChannelId { get; set; }

    /// <summary>
    /// Level-up announcement channel ID
    /// </summary>
    [JsonPropertyName("levelup_channel")]
    public string? LevelupChannelId { get; set; }

    /// <summary>
    /// Channels excluded from XP gain
    /// </summary>
    [JsonPropertyName("xp_excluded_channels")]
    public List<string> XpExcludedChannels { get; set; } = new();
}

/// <summary>
/// Represents XP and leveling configurations
/// </summary>
public class XpSettings
{
    /// <summary>
    /// Base XP for messages
    /// </summary>
    [JsonPropertyName("message_xp")]
    public int MessageXp { get; set; } = 5;

    /// <summary>
    /// XP per minute in voice channels
    /// </summary>
    [JsonPropertyName("voice_xp_per_minute")]
    public int VoiceXpPerMinute { get; set; } = 2;

    /// <summary>
    /// XP for reactions
    /// </summary>
    [JsonPropertyName("reaction_xp")]
    public int ReactionXp { get; set; } = 1;

    /// <summary>
    /// Cooldown between XP gains in seconds
    /// </summary>
    [JsonPropertyName("xp_cooldown")]
    public int XpCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Level roles configuration
    /// </summary>
    [JsonPropertyName("level_roles")]
    public List<LevelRole> LevelRoles { get; set; } = new();

    /// <summary>
    /// Whether to announce level ups
    /// </summary>
    [JsonPropertyName("announce_levelup")]
    public bool AnnounceLevelUp { get; set; } = true;

    /// <summary>
    /// XP multipliers for roles, users, etc.
    /// Keys in the format: "role_{roleId}", "user_{userId}", etc.
    /// </summary>
    [JsonPropertyName("level_multipliers")]
    public Dictionary<string, double> LevelMultipliers { get; set; } = new();
}

/// <summary>
/// Represents moderation settings
/// </summary>
public class ModerationSettings
{
    /// <summary>
    /// Warning expiry time in days (null for no expiry)
    /// </summary>
    [JsonPropertyName("warning_expiry_days")]
    public int? WarningExpiryDays { get; set; }

    /// <summary>
    /// Auto-mute thresholds
    /// </summary>
    [JsonPropertyName("mute_thresholds")]
    public List<MuteThreshold> MuteThresholds { get; set; } = new();

    /// <summary>
    /// Roles that can manage warnings
    /// </summary>
    [JsonPropertyName("warning_manager_roles")]
    public List<string> WarningManagerRoles { get; set; } = new();
}

/// <summary>
/// Represents welcome message settings
/// </summary>
public class WelcomeSettings
{
    /// <summary>
    /// Welcome message template
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Whether to use embed for welcome
    /// </summary>
    [JsonPropertyName("use_embed")]
    public bool UseEmbed { get; set; } = true;

    /// <summary>
    /// Embed color (hex code)
    /// </summary>
    [JsonPropertyName("embed_color")]
    public string EmbedColor { get; set; } = "#5865F2";
}

/// <summary>
/// Represents an auto-role configuration
/// </summary>
public class AutoRole
{
    /// <summary>
    /// Role ID to assign
    /// </summary>
    [JsonPropertyName("role_id")]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Delay before assigning in minutes (0 for immediate)
    /// </summary>
    [JsonPropertyName("delay_minutes")]
    public int DelayMinutes { get; set; }
}

/// <summary>
/// Represents a reaction role configuration
/// </summary>
public class ReactionRole
{
    /// <summary>
    /// Message ID for the reaction role
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Role ID to assign
    /// </summary>
    [JsonPropertyName("role_id")]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Emoji ID or unicode emoji
    /// </summary>
    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = string.Empty;
}

/// <summary>
/// Represents a level role configuration
/// </summary>
public class LevelRole
{
    /// <summary>
    /// Role ID to assign
    /// </summary>
    [JsonPropertyName("role_id")]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Level required for this role
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// Whether to stack with lower level roles
    /// </summary>
    [JsonPropertyName("stack")]
    public bool Stack { get; set; } = true;
}

/// <summary>
/// Represents an auto-mute threshold
/// </summary>
public class MuteThreshold
{
    /// <summary>
    /// Number of warnings that trigger this threshold
    /// </summary>
    [JsonPropertyName("warning_count")]
    public int WarningCount { get; set; }

    /// <summary>
    /// Duration of mute in minutes
    /// </summary>
    [JsonPropertyName("mute_duration")]
    public int MuteDurationMinutes { get; set; }
}

/// <summary>
/// Represents a custom command
/// </summary>
public class CustomCommand
{
    /// <summary>
    /// Command trigger
    /// </summary>
    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = string.Empty;

    /// <summary>
    /// Command response
    /// </summary>
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use embed for response
    /// </summary>
    [JsonPropertyName("use_embed")]
    public bool UseEmbed { get; set; }
}

/// <summary>
/// Represents feature toggles and settings
/// </summary>
public class FeatureSettings
{
    /// <summary>
    /// Whether XP system is enabled
    /// </summary>
    [JsonPropertyName("xp_enabled")]
    public bool XpEnabled { get; set; } = true;

    /// <summary>
    /// Whether welcome messages are enabled
    /// </summary>
    [JsonPropertyName("welcome_enabled")]
    public bool WelcomeEnabled { get; set; } = true;

    /// <summary>
    /// Whether moderation features are enabled
    /// </summary>
    [JsonPropertyName("moderation_enabled")]
    public bool ModerationEnabled { get; set; } = true;

    /// <summary>
    /// Whether custom commands are enabled
    /// </summary>
    [JsonPropertyName("custom_commands_enabled")]
    public bool CustomCommandsEnabled { get; set; } = true;

    /// <summary>
    /// Additional feature flags
    /// </summary>
    [JsonPropertyName("flags")]
    public Dictionary<string, bool> Flags { get; set; } = new();
}