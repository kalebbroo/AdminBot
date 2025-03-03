using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace AdminBot.Models;

/// <summary>
/// Represents a user's profile in the database.
/// Maps to the 'user_profiles' table in Supabase.
/// </summary>
public class UserProfile : BaseModel
{
    /// <summary>
    /// Discord user ID (used as primary key)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Guild-specific profiles for this user
    /// </summary>
    [JsonPropertyName("guild_profiles")]
    public List<GuildProfile> GuildProfiles { get; set; } = new();

    /// <summary>
    /// When the profile was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user was active
    /// </summary>
    [JsonPropertyName("last_active")]
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of warnings received across all guilds
    /// </summary>
    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }

    /// <summary>
    /// Gets a guild profile by guild ID
    /// </summary>
    public GuildProfile? GetGuildProfile(string guildId)
    {
        return GuildProfiles.FirstOrDefault(p => p.GuildId == guildId);
    }

    /// <summary>
    /// Gets a guild profile by guild ID, creating it if it doesn't exist
    /// </summary>
    public GuildProfile GetOrCreateGuildProfile(string guildId)
    {
        var profile = GetGuildProfile(guildId);
        if (profile == null)
        {
            profile = new GuildProfile
            {
                GuildId = guildId,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow
            };
            GuildProfiles.Add(profile);
        }
        return profile;
    }
}

/// <summary>
/// Represents a user's profile specific to a guild
/// Stored as JSONB array in user_profiles.guild_profiles
/// </summary>
public class GuildProfile
{
    /// <summary>
    /// Discord guild ID
    /// </summary>
    [JsonPropertyName("guild_id")]
    public string GuildId { get; set; } = string.Empty;

    /// <summary>
    /// Current XP in this guild
    /// </summary>
    [JsonPropertyName("xp")]
    public int Xp { get; set; }

    /// <summary>
    /// Current level in this guild
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// Total messages sent in this guild
    /// </summary>
    [JsonPropertyName("messages_sent")]
    public int MessagesSent { get; set; }

    /// <summary>
    /// Total voice minutes in this guild
    /// </summary>
    [JsonPropertyName("voice_minutes")]
    public int VoiceMinutes { get; set; }

    /// <summary>
    /// When this guild profile was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user was active in this guild
    /// </summary>
    [JsonPropertyName("last_active")]
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Profile customization data
    /// </summary>
    [JsonPropertyName("profile_data")]
    public Dictionary<string, object>? ProfileData { get; set; }
}