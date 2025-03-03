using System.Text.Json.Serialization;

namespace AdminBot.Models;

/// <summary>
/// Represents a server-wide event.
/// Maps to the 'guild_events' table in Supabase.
/// </summary>
public class GuildEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Discord ID of the guild
    /// </summary>
    [JsonPropertyName("guild_id")]
    public string GuildId { get; set; } = string.Empty;

    /// <summary>
    /// Type of event
    /// </summary>
    [JsonPropertyName("type")]
    public EventType Type { get; set; }

    /// <summary>
    /// Name/title of the event
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Event description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// When the event starts
    /// </summary>
    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the event ends
    /// </summary>
    [JsonPropertyName("end_time")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Discord ID of the user who created the event
    /// </summary>
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the event was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the event
    /// </summary>
    [JsonPropertyName("status")]
    public EventStatus Status { get; set; } = EventStatus.Scheduled;

    /// <summary>
    /// XP multiplier for the event (1.0 = normal)
    /// </summary>
    [JsonPropertyName("xp_multiplier")]
    public decimal XpMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Specific activities that get the multiplier
    /// </summary>
    [JsonPropertyName("activity_types")]
    public List<ActivityType> ActivityTypes { get; set; } = new();

    /// <summary>
    /// Role IDs that get additional bonuses
    /// </summary>
    [JsonPropertyName("bonus_roles")]
    public List<RoleBonus> BonusRoles { get; set; } = new();

    /// <summary>
    /// Channel IDs where the event is active
    /// </summary>
    [JsonPropertyName("channel_ids")]
    public List<string> ChannelIds { get; set; } = new();

    /// <summary>
    /// Additional conditions for the event
    /// </summary>
    [JsonPropertyName("conditions")]
    public EventConditions? Conditions { get; set; }

    /// <summary>
    /// Event rewards configuration
    /// </summary>
    [JsonPropertyName("rewards")]
    public EventRewards? Rewards { get; set; }

    /// <summary>
    /// Notification settings for the event
    /// </summary>
    [JsonPropertyName("notifications")]
    public EventNotifications Notifications { get; set; } = new();

    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Whether the event is currently active
    /// </summary>
    [JsonIgnore]
    public bool IsActive => Status == EventStatus.Active &&
                          DateTime.UtcNow >= StartTime &&
                          DateTime.UtcNow <= EndTime;

    /// <summary>
    /// Gets the effective multiplier for a given user and activity
    /// </summary>
    public decimal GetEffectiveMultiplier(string userId, ActivityType activity, IEnumerable<string> userRoles)
    {
        if (!IsActive || !ActivityTypes.Contains(activity))
        {
            return 1.0m;
        }

        var multiplier = XpMultiplier;

        // Add role bonuses
        foreach (var roleBonus in BonusRoles)
        {
            if (userRoles.Contains(roleBonus.RoleId))
            {
                multiplier *= roleBonus.Multiplier;
            }
        }

        return multiplier;
    }
}

/// <summary>
/// Types of server events
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType
{
    /// <summary>
    /// General XP boost event
    /// </summary>
    XpBoost,

    /// <summary>
    /// Activity-specific boost
    /// </summary>
    ActivityBoost,

    /// <summary>
    /// Special reward event
    /// </summary>
    RewardEvent,

    /// <summary>
    /// Competition or challenge
    /// </summary>
    Challenge,

    /// <summary>
    /// Seasonal or holiday event
    /// </summary>
    Seasonal
}

/// <summary>
/// Status of an event
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventStatus
{
    /// <summary>
    /// Event is scheduled but not started
    /// </summary>
    Scheduled,

    /// <summary>
    /// Event is currently running
    /// </summary>
    Active,

    /// <summary>
    /// Event has ended
    /// </summary>
    Ended,

    /// <summary>
    /// Event was cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Types of activities that can earn XP
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityType
{
    /// <summary>
    /// Sending messages
    /// </summary>
    Messages,

    /// <summary>
    /// Voice channel activity
    /// </summary>
    Voice,

    /// <summary>
    /// Adding reactions
    /// </summary>
    Reactions,

    /// <summary>
    /// All activity types
    /// </summary>
    All
}

/// <summary>
/// Represents a role-specific XP bonus
/// </summary>
public class RoleBonus
{
    /// <summary>
    /// Discord role ID
    /// </summary>
    [JsonPropertyName("role_id")]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Additional multiplier for this role
    /// </summary>
    [JsonPropertyName("multiplier")]
    public decimal Multiplier { get; set; } = 1.0m;
}

/// <summary>
/// Additional conditions for event participation
/// </summary>
public class EventConditions
{
    /// <summary>
    /// Minimum level required
    /// </summary>
    [JsonPropertyName("min_level")]
    public int? MinLevel { get; set; }

    /// <summary>
    /// Required roles (any)
    /// </summary>
    [JsonPropertyName("required_roles")]
    public List<string> RequiredRoles { get; set; } = new();

    /// <summary>
    /// Maximum participants
    /// </summary>
    [JsonPropertyName("max_participants")]
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Whether users need to opt-in
    /// </summary>
    [JsonPropertyName("requires_optin")]
    public bool RequiresOptIn { get; set; }
}

/// <summary>
/// Configuration for event rewards
/// </summary>
public class EventRewards
{
    /// <summary>
    /// Role ID to award
    /// </summary>
    [JsonPropertyName("role_id")]
    public string? RoleId { get; set; }

    /// <summary>
    /// Custom title to award
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Base XP reward
    /// </summary>
    [JsonPropertyName("base_xp")]
    public int? BaseXp { get; set; }

    /// <summary>
    /// Bonus rewards for top participants
    /// </summary>
    [JsonPropertyName("top_rewards")]
    public Dictionary<int, int> TopRewards { get; set; } = new();
}

/// <summary>
/// Configuration for event notifications
/// </summary>
public class EventNotifications
{
    /// <summary>
    /// Whether to announce start
    /// </summary>
    [JsonPropertyName("announce_start")]
    public bool AnnounceStart { get; set; } = true;

    /// <summary>
    /// Whether to announce end
    /// </summary>
    [JsonPropertyName("announce_end")]
    public bool AnnounceEnd { get; set; } = true;

    /// <summary>
    /// Channel to send announcements
    /// </summary>
    [JsonPropertyName("announcement_channel")]
    public string? AnnouncementChannel { get; set; }

    /// <summary>
    /// Role to ping for announcements
    /// </summary>
    [JsonPropertyName("ping_role")]
    public string? PingRole { get; set; }

    /// <summary>
    /// Whether to send reminders
    /// </summary>
    [JsonPropertyName("send_reminders")]
    public bool SendReminders { get; set; }

    /// <summary>
    /// Minutes before event to send reminder
    /// </summary>
    [JsonPropertyName("reminder_minutes")]
    public int ReminderMinutes { get; set; } = 30;
}