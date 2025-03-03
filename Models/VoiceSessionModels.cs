using System.Text.Json.Serialization;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.Voice;

namespace AdminBot.Models;

/// <summary>
/// Represents a voice channel session.
/// Maps to the 'voice_sessions' table in Supabase.
/// </summary>
public class VoiceSession
{
    /// <summary>
    /// Unique identifier for the session
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Discord ID of the user
    /// </summary>
    [JsonPropertyName("user_id")]
    public ulong UserId { get; set; }

    /// <summary>
    /// Discord ID of the guild
    /// </summary>
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }

    /// <summary>
    /// Discord ID of the voice channel
    /// </summary>
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }

    /// <summary>
    /// Current voice client for this session
    /// </summary>
    [JsonIgnore]
    public VoiceClient? VoiceClient { get; set; }

    /// <summary>
    /// When the session started
    /// </summary>
    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session ended (null if ongoing)
    /// </summary>
    [JsonPropertyName("end_time")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Last time activity was detected
    /// </summary>
    [JsonPropertyName("last_activity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current state of the session
    /// </summary>
    [JsonPropertyName("state")]
    public VoiceState State { get; set; } = VoiceState.Connecting;

    /// <summary>
    /// Total time spent speaking
    /// </summary>
    [JsonPropertyName("speaking_time")]
    public TimeSpan SpeakingTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Total XP earned in this session
    /// </summary>
    [JsonPropertyName("xp_earned")]
    public int XpEarned { get; set; }

    /// <summary>
    /// Whether the user is currently speaking
    /// </summary>
    [JsonPropertyName("is_speaking")]
    public bool IsSpeaking { get; set; }

    /// <summary>
    /// Whether the user is muted
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool IsMuted { get; set; }

    /// <summary>
    /// Whether the user is deafened
    /// </summary>
    [JsonPropertyName("is_deafened")]
    public bool IsDeafened { get; set; }

    /// <summary>
    /// Additional session metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets whether the session is currently active
    /// </summary>
    [JsonIgnore]
    public bool IsActive => EndTime == null &&
                          State != VoiceState.Disconnected &&
                          VoiceClient != null;

    /// <summary>
    /// Gets the total duration of the session
    /// </summary>
    [JsonIgnore]
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

    /// <summary>
    /// Updates the session's activity timestamp
    /// </summary>
    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the state of a voice session
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VoiceState
{
    /// <summary>
    /// Session is connecting to voice
    /// </summary>
    Connecting,

    /// <summary>
    /// Session is connected and ready
    /// </summary>
    Connected,

    /// <summary>
    /// Session is reconnecting after disconnect
    /// </summary>
    Reconnecting,

    /// <summary>
    /// Session is disconnected
    /// </summary>
    Disconnected
}