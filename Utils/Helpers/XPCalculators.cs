using AdminBot.Models;
using AdminBot.Models.Configuration;
using System.Collections.Generic;

namespace AdminBot.Utils.Helpers;

/// <summary>
/// Provides XP calculation and leveling logic.
/// Handles XP awards, level requirements, and multipliers.
/// </summary>
public class XpCalculator(XpSettings config)
{
    private readonly XpSettings _config = config;
    private readonly Random _random = new Random();

    // Default values if not specified in config
    private const int DEFAULT_BASE_XP = 10;
    private const double DEFAULT_MULTIPLIER = 1.5;
    private const int DEFAULT_MIN_MESSAGE_LENGTH = 20;

    /// <summary>
    /// Calculates XP required for a specific level.
    /// Uses exponential scaling formula.
    /// </summary>
    public int GetRequiredXp(int level)
    {
        // Base formula: baseXp * (multiplier^(level-1))
        var baseXp = _config.MessageXp > 0 ? _config.MessageXp : DEFAULT_BASE_XP;
        var multiplier = GetLevelMultiplier("base") ?? DEFAULT_MULTIPLIER;

        return (int)(baseXp * Math.Pow(multiplier, level - 1));
    }

    /// <summary>
    /// Calculates XP for a message.
    /// Takes into account length, content, and active multipliers.
    /// </summary>
    public int CalculateMessageXp(
        string content,
        string userId,
        IEnumerable<string> roleIds,
        IEnumerable<GuildEvent> activeEvents)
    {
        // Base XP for message
        var baseXp = _config.MessageXp;

        // Length bonus (every 20 chars over minimum = +1 XP, up to +5)
        var lengthBonus = Math.Min(5, Math.Max(0, content.Length - DEFAULT_MIN_MESSAGE_LENGTH) / 20);

        // Apply random variation (±20%)
        var variation = 1 + (_random.NextDouble() * 0.4 - 0.2);

        // Calculate raw XP
        var rawXp = (baseXp + lengthBonus) * variation;

        // Apply multipliers
        return (int)Math.Ceiling(ApplyMultipliers(rawXp, userId, roleIds, activeEvents));
    }

    /// <summary>
    /// Calculates XP for voice activity.
    /// Takes into account duration and active multipliers.
    /// </summary>
    public int CalculateVoiceXp(
        int minutes,
        bool isSpeaking,
        string userId,
        IEnumerable<string> roleIds,
        IEnumerable<GuildEvent> activeEvents)
    {
        // Base XP per minute
        var baseXp = _config.VoiceXpPerMinute;

        // Speaking bonus (25% extra if actively speaking)
        var speakingMultiplier = isSpeaking ? 1.25 : 1.0;

        // Calculate raw XP
        var rawXp = baseXp * minutes * speakingMultiplier;

        // Apply multipliers
        return (int)Math.Ceiling(ApplyMultipliers(rawXp, userId, roleIds, activeEvents));
    }

    /// <summary>
    /// Calculates XP for reactions.
    /// Takes into account type and active multipliers.
    /// </summary>
    public int CalculateReactionXp(
        bool isCustomEmoji,
        string userId,
        IEnumerable<string> roleIds,
        IEnumerable<GuildEvent> activeEvents)
    {
        // Base XP for reaction
        var baseXp = _config.ReactionXp;

        // Custom emoji bonus (50% extra for custom emoji)
        var emojiMultiplier = isCustomEmoji ? 1.5 : 1.0;

        // Calculate raw XP
        var rawXp = baseXp * emojiMultiplier;

        // Apply multipliers
        return (int)Math.Ceiling(ApplyMultipliers(rawXp, userId, roleIds, activeEvents));
    }

    /// <summary>
    /// Checks if user should level up based on current XP.
    /// </summary>
    public bool ShouldLevelUp(int currentXp, int currentLevel)
    {
        return currentXp >= GetRequiredXp(currentLevel + 1);
    }

    /// <summary>
    /// Gets the progress towards next level as a percentage.
    /// </summary>
    public double GetLevelProgress(int currentXp, int currentLevel)
    {
        var currentLevelXp = GetRequiredXp(currentLevel);
        var nextLevelXp = GetRequiredXp(currentLevel + 1);
        var requiredXp = nextLevelXp - currentLevelXp;
        var progress = currentXp - currentLevelXp;

        return Math.Min(1.0, Math.Max(0.0, progress / (double)requiredXp));
    }

    /// <summary>
    /// Gets XP remaining until next level.
    /// </summary>
    public int GetRemainingXp(int currentXp, int currentLevel)
    {
        var nextLevelXp = GetRequiredXp(currentLevel + 1);
        return Math.Max(0, nextLevelXp - currentXp);
    }

    /// <summary>
    /// Applies all active multipliers to XP amount.
    /// </summary>
    private double ApplyMultipliers(
        double baseXp,
        string userId,
        IEnumerable<string> roleIds,
        IEnumerable<GuildEvent> activeEvents)
    {
        var multiplier = 1.0;

        // Role multipliers
        foreach (var roleId in roleIds)
        {
            // Check if we have a multiplier for this role
            double roleMultiplier;
            if (_config.LevelMultipliers != null && 
                _config.LevelMultipliers.TryGetValue($"role_{roleId}", out var multiplierObj) && 
                multiplierObj is double roleMultValue)
            {
                roleMultiplier = roleMultValue;
                multiplier *= roleMultiplier;
            }
        }

        // Event multipliers
        foreach (var evt in activeEvents.Where(e => e.IsActive))
        {
            // Check if user meets event conditions
            if (evt.Conditions?.MinLevel.HasValue == true)
            {
                // Skip if user doesn't meet level requirement
                continue;
            }

            if (evt.Conditions?.RequiredRoles.Any() == true &&
                !evt.Conditions.RequiredRoles.Any(r => roleIds.Contains(r)))
            {
                // Skip if user doesn't have required role
                continue;
            }

            multiplier *= (double)evt.XpMultiplier;
        }

        // User-specific multipliers (e.g., boosters, special status)
        double userMultiplier;
        if (_config.LevelMultipliers != null && 
            _config.LevelMultipliers.TryGetValue($"user_{userId}", out var userMultObj) && 
            userMultObj is double userMultValue)
        {
            userMultiplier = userMultValue;
            multiplier *= userMultiplier;
        }

        return baseXp * multiplier;
    }

    /// <summary>
    /// Gets a level multiplier by key from config.
    /// </summary>
    private double? GetLevelMultiplier(string key)
    {
        if (_config.LevelMultipliers == null || !_config.LevelMultipliers.TryGetValue(key, out var value))
        {
            return null;
        }
        
        return value is double doubleValue ? doubleValue : null;
    }

    /// <summary>
    /// Calculates rewards for a specific level.
    /// </summary>
    public LevelRewards GetLevelRewards(int level)
    {
        var rewards = new LevelRewards
        {
            Level = level,
            XpBonus = (int)(GetRequiredXp(level) * 0.1) // 10% of level requirement as bonus
        };

        // Add role rewards if configured
        foreach (var levelRole in _config.LevelRoles)
        {
            if (level >= levelRole.Level)
            {
                rewards.RoleIds.Add(levelRole.RoleId);
            }
        }

        return rewards;
    }
}

/// <summary>
/// Represents rewards for reaching a level.
/// </summary>
public class LevelRewards
{
    /// <summary>
    /// The level these rewards are for
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Bonus XP awarded
    /// </summary>
    public int XpBonus { get; set; }

    /// <summary>
    /// Role IDs to award
    /// </summary>
    public List<string> RoleIds { get; set; } = new();
}