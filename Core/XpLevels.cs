using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminBot.Core
{
    internal class XpLevels
    {
        private readonly DiscordSocketClient _client;
        private readonly Database _database;
        private readonly int BaseXp;
        private readonly double XpMultiplier;
        private readonly Dictionary<ulong, DateTime> _lastXpGain = new();

        /* XpLevels constructor
         * A constructor that takes a Database object as a parameter.
         * This runs when a new instance of XpLevels is created.
         * Sets the _database field to the passed Database object.
         */
        public XpLevels(DiscordSocketClient client, Database database)
        {
            _client = client;
            _database = database;
            BaseXp = int.Parse(Environment.GetEnvironmentVariable("BASE_XP") ?? "100");
            XpMultiplier = double.Parse(Environment.GetEnvironmentVariable("XP_MULTIPLIER") ?? "1.5");
        }
        public int XpToNextLevel(int level)
        {

            return (int)(BaseXp * Math.Pow(XpMultiplier, level - 1));
        }

        public async Task AddXp(ulong userId, ulong guildId, int xpToAdd)
        {
            var displayName = _client.GetGuild(guildId).GetUser(userId).DisplayName ?? "none";// maybe make this userid?
            var username = _client.GetGuild(guildId).GetUser(userId).Username ?? "none";
            if (_lastXpGain.TryGetValue(userId, out var lastGainTime))
            {
                TimeSpan cooldown = TimeSpan.FromSeconds(5);
                if ((DateTime.UtcNow - lastGainTime) < cooldown)
                {
                    Console.WriteLine($"Cooldown in effect for user {username}. XP not added.");
                    return;
                }
                else
                {
                    Console.WriteLine($"Added {xpToAdd}XP to user: {username}");
                }
            }

            var (userData, collection) = await _database.GetUserData(userId, guildId, username);
            if (userData != null)
            {
                var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId);
                if (guildData != null)
                {
                    guildData.Xp += xpToAdd; // Add XP to the total

                    // Check if the total XP is equal to or exceeds the amount needed for the next level
                    while (guildData.Xp >= XpToNextLevel(guildData.Level + 1))
                    {
                        await LevelUp(guildData, userId); // Level up if enough XP has been accumulated
                    }

                    _lastXpGain[userId] = DateTime.UtcNow;
                    await _database.UpdateAddUser(userData, collection); // Save updated user data
                }
            }
        }

        public async Task LevelUp(GuildData guildData, ulong userId)
        {
            // Use XpToNextLevel to get the XP needed for the current level
            // TODO: Add this info to the level up message
            int xpNextLevel = XpToNextLevel(guildData.Level);

            // Increment the user's level
            guildData.Level++;
            ulong guildId = guildData.GuildId;
            var displayName = _client.GetGuild(guildId).GetUser(userId).DisplayName ?? "none";
            // TODO: Add a message to the user that they leveled up maybe even an image
            Console.WriteLine($"User {displayName} leveled up to level {guildData.Level}.");
        }
    }

}
