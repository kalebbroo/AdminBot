using System.Text.Json.Serialization;
using NetCord.Gateway;
using Microsoft.Extensions.Logging;

namespace AdminBot.Models.Configuration;

/// <summary>
/// Represents global bot configuration settings.
/// Handles application-wide settings and external service connections.
/// </summary>
public sealed class BotConfig : IConfiguration, IFeatureConfiguration
{
    /// <summary>
    /// Discord-specific configuration.
    /// </summary>
    public sealed class DiscordConfig
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("intents")]
        public GatewayIntents Intents { get; set; } = GatewayIntents.All;

        [JsonPropertyName("status_message")]
        public string StatusMessage { get; set; } = "Serving your community!";

        [JsonPropertyName("command_prefix")]
        public string CommandPrefix { get; set; } = "!";
    }

    /// <summary>
    /// Database connection configuration.
    /// </summary>
    public sealed class DatabaseConfig
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("max_connections")]
        public int MaxConnections { get; set; } = 50;

        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Redis cache configuration.
    /// </summary>
    public sealed class CacheConfig
    {
        [JsonPropertyName("connection_string")]
        public string ConnectionString { get; set; } = "localhost:6379";

        [JsonPropertyName("instance_name")]
        public string InstanceName { get; set; } = "AdminBot";

        [JsonPropertyName("default_ttl_minutes")]
        public int DefaultTtlMinutes { get; set; } = 30;
    }

    /// <summary>
    /// Logging configuration.
    /// </summary>
    public sealed class LogConfig
    {
        [JsonPropertyName("level")]
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        [JsonPropertyName("file_path")]
        public string FilePath { get; set; } = "logs/bot.log";

        [JsonPropertyName("retain_days")]
        public int RetainDays { get; set; } = 30;
    }

    [JsonPropertyName("discord")]
    public DiscordConfig Discord { get; set; } = new();

    [JsonPropertyName("database")]
    public DatabaseConfig Database { get; set; } = new();

    [JsonPropertyName("cache")]
    public CacheConfig Cache { get; set; } = new();

    [JsonPropertyName("logging")]
    public LogConfig Logging { get; set; } = new();

    [JsonPropertyName("features")]
    public Dictionary<string, bool> Features { get; set; } = new();

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Loads configuration from environment variables.
    /// </summary>
    public static BotConfig LoadFromEnvironment()
    {
        return new BotConfig
        {
            Discord = new DiscordConfig
            {
                Token = GetRequiredEnv("DISCORD_TOKEN"),
                Intents = ParseIntents(GetEnv("DISCORD_INTENTS", "All")),
                StatusMessage = GetEnv("DISCORD_STATUS", "Serving your community!"),
                CommandPrefix = GetEnv("DISCORD_PREFIX", "!")
            },
            Database = new DatabaseConfig
            {
                Url = GetRequiredEnv("DATABASE_URL"),
                Key = GetRequiredEnv("DATABASE_KEY"),
                MaxConnections = GetEnvInt("DATABASE_MAX_CONNECTIONS", 50),
                TimeoutSeconds = GetEnvInt("DATABASE_TIMEOUT", 30)
            },
            Cache = new CacheConfig
            {
                ConnectionString = GetEnv("REDIS_CONNECTION", "localhost:6379"),
                InstanceName = GetEnv("REDIS_INSTANCE", "AdminBot"),
                DefaultTtlMinutes = GetEnvInt("REDIS_TTL", 30)
            },
            Logging = new LogConfig
            {
                MinimumLevel = ParseLogLevel(GetEnv("LOG_LEVEL", "Information")),
                FilePath = GetEnv("LOG_PATH", "logs/bot.log"),
                RetainDays = GetEnvInt("LOG_RETAIN_DAYS", 30)
            }
        };
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public bool Validate(out string[] errors)
    {
        List<string> errorList = new();

        if (string.IsNullOrEmpty(Discord.Token))
            errorList.Add("Discord token is required");

        if (string.IsNullOrEmpty(Database.Url))
            errorList.Add("Database URL is required");

        if (string.IsNullOrEmpty(Database.Key))
            errorList.Add("Database key is required");

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

    #region Helper Methods

    private static string GetRequiredEnv(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required environment variable '{key}' is not set");
        }
        return value;
    }

    private static string GetEnv(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    private static int GetEnvInt(string key, int defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    private static GatewayIntents ParseIntents(string intents)
    {
        return intents.ToLower() switch
        {
            "all" => GatewayIntents.All,
            "none" => GatewayIntents.AllNonPrivileged,
            _ => (GatewayIntents)Enum.Parse(typeof(GatewayIntents), intents)
        };
    }

    private static LogLevel ParseLogLevel(string level)
    {
        return Enum.TryParse<LogLevel>(level, true, out var result)
            ? result
            : LogLevel.Information;
    }

    #endregion
}