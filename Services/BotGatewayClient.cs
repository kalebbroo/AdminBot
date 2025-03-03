using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminBot.Services
{
    /// <summary>
    /// Information about a shard
    /// </summary>
    public class ShardInfo
    {
        public int Id { get; set; }
        public ShardStatus Status { get; set; }
        public int GuildCount { get; set; }
        public int LatencyMs { get; set; }
    }

    /// <summary>
    /// Status of a shard
    /// </summary>
    public enum ShardStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }

    /// <summary>
    /// Wrapper around NetCord's GatewayClient to provide additional functionality
    /// and simplify interaction with the Discord Gateway API.
    /// </summary>
    public class BotGatewayClient(ILogger<BotGatewayClient> logger, IEntityToken token,
        GatewayClientConfiguration configuration) : GatewayClient(token, configuration)
    {
        private readonly ILogger<BotGatewayClient> _logger = logger;

        /// <summary>
        /// Gets the count of available guilds
        /// </summary>
        public int GuildCount => Cache.Guilds.Count;

        /// <summary>
        /// Gets all available guilds
        /// </summary>
        public IReadOnlyDictionary<ulong, Guild> Guilds
        {
            get
            {
                try
                {
                    return Cache.Guilds;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get guilds");
                    return new Dictionary<ulong, Guild>();
                }
            }
        }

        /// <summary>
        /// Gets information about the shards
        /// </summary>
        public IReadOnlyDictionary<int, ShardInfo> ShardInfo
        {
            get
            {
                try
                {
                    var shardInfo = new Dictionary<int, ShardInfo>();
                    // If we have shard information available, convert it
                    shardInfo.Add(0, new ShardInfo
                    {
                        Id = 0,
                        Status = ShardStatus.Connected,
                        GuildCount = GuildCount,
                        LatencyMs = (int)Latency.TotalMilliseconds
                    });

                    return shardInfo;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get shard info");
                    return new Dictionary<int, ShardInfo>();
                }
            }
        }

        /// <summary>
        /// Gets a guild by ID
        /// </summary>
        public Guild? GetGuild(ulong guildId)
        {
            try
            {
                return Cache.Guilds.TryGetValue(guildId, out var guild) ? guild : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get guild {GuildId}", guildId);
                return null;
            }
        }

        ///<summary>
        ///</summary>
        ///<returns>
        ///</returns>
        public List<Guild> GetGuilds()
        {
            try
            {
                return Cache.Guilds.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get guilds");
                return new List<Guild>();
            }
        }

        /// <summary>
        /// Gets the current user (bot) information.
        /// </summary>
        /// <returns>The current user info or null if not available.</returns>
        public CurrentUser? GetUser()
        {
            try
            {
                return Cache.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current user");
                return null;
            }
        }
    }
}
