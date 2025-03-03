using System.Text.Json.Serialization;
using Supabase.Postgrest.Models;

namespace AdminBot.Models;

/// <summary>
/// Represents a warning issued to a user.
/// Maps to the 'warnings' table in Supabase.
/// </summary>
public class Warning : BaseModel
{
    /// <summary>
    /// Unique identifier for the warning
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Discord ID of the warned user
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Discord ID of the guild where warning was issued
    /// </summary>
    [JsonPropertyName("guild_id")]
    public string GuildId { get; set; } = string.Empty;

    /// <summary>
    /// Discord ID of the admin who issued the warning
    /// </summary>
    [JsonPropertyName("moderator_id")]
    public string ModeratorId { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the warning
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// When the warning expires
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the warning was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Check if the warning has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}

/// <summary>
/// Represents the status of a warning
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WarningStatus
{
    /// <summary>
    /// Warning is active and counts towards thresholds
    /// </summary>
    Active,

    /// <summary>
    /// Warning has expired
    /// </summary>
    Expired,

    /// <summary>
    /// Warning was pardoned by an admin
    /// </summary>
    Pardoned,

    /// <summary>
    /// Warning was appealed and removed
    /// </summary>
    Appealed,

    /// <summary>
    /// Warning was revoked by the issuing admin
    /// </summary>
    Revoked
}

/// <summary>
/// Represents a moderation action taken with a warning
/// </summary>
public class ModerationAction
{
    /// <summary>
    /// Type of action taken
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// When the action was taken
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the action (if applicable)
    /// </summary>
    [JsonPropertyName("duration")]
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// When the action expires (if applicable)
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Additional data about the action
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Represents an appeal against a warning
/// </summary>
public class WarningAppeal
{
    /// <summary>
    /// ID of user making the appeal
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When the appeal was made
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reason given for the appeal
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the appeal
    /// </summary>
    [JsonPropertyName("status")]
    public AppealStatus Status { get; set; } = AppealStatus.Pending;

    /// <summary>
    /// When the appeal status was last updated
    /// </summary>
    [JsonPropertyName("status_updated_at")]
    public DateTime? StatusUpdatedAt { get; set; }

    /// <summary>
    /// ID of admin who handled the appeal
    /// </summary>
    [JsonPropertyName("handled_by")]
    public string? HandledBy { get; set; }

    /// <summary>
    /// Response to the appeal
    /// </summary>
    [JsonPropertyName("response")]
    public string? Response { get; set; }
}

/// <summary>
/// Represents the status of an appeal
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppealStatus
{
    /// <summary>
    /// Appeal is waiting for review
    /// </summary>
    Pending,

    /// <summary>
    /// Appeal was approved
    /// </summary>
    Approved,

    /// <summary>
    /// Appeal was denied
    /// </summary>
    Denied,

    /// <summary>
    /// Appeal was cancelled
    /// </summary>
    Cancelled
}