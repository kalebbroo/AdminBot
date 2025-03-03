using System.Text.Json.Serialization;

namespace AdminBot.Models.Configuration;

/// <summary>
/// Base interface for all configuration types.
/// </summary>
public interface IConfiguration
{
    /// <summary>
    /// When this configuration was last updated.
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    bool Validate(out string[] errors);
}

/// <summary>
/// Interface for configurations that support feature flags.
/// </summary>
public interface IFeatureConfiguration
{
    /// <summary>
    /// Dictionary of enabled/disabled features.
    /// </summary>
    Dictionary<string, bool> Features { get; set; }

    /// <summary>
    /// Checks if a feature is enabled.
    /// </summary>
    bool IsFeatureEnabled(string featureKey);
}

/// <summary>
/// Interface for configurations that have channel settings.
/// </summary>
public interface IChannelConfiguration
{
    /// <summary>
    /// Dictionary of channel IDs and their purposes.
    /// </summary>
    Dictionary<string, string> ChannelIds { get; set; }

    /// <summary>
    /// Gets a channel ID for a specific purpose.
    /// </summary>
    string? GetChannelId(string purpose);
}