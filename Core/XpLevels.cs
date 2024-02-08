using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminBot.Core
{
    internal class XpLevels
    {
        private readonly Database _database;
        private readonly int BaseXp;
        private readonly double XpMultiplier;
        private readonly Dictionary<ulong, DateTime> _lastXpGain = new();

        /* XpLevels constructor
         * A constructor that takes a Database object as a parameter.
         * This runs when a new instance of XpLevels is created.
         * Sets the _database field to the passed Database object.
         */
        public XpLevels(Database database)
        {
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
            // Check if the user has gained XP within the last 5 seconds
            // This is to prevent spamming and abuse
            if (_lastXpGain.TryGetValue(userId, out var lastGainTime))
            {
                TimeSpan cooldown = TimeSpan.FromSeconds(5); // 5-second cooldown
                if ((DateTime.UtcNow - lastGainTime) < cooldown)
                {
                    Console.WriteLine($"Cooldown in effect for user {userId}. XP not added.");
                    return; // Return if the cooldown is still in effect
                }
            }
            var userData = await _database.GetUserData(userId, guildId);
            if (userData != null)
            {
                var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId);
                if (guildData != null)
                {
                    guildData.Xp += xpToAdd;
                    if (guildData.Xp >= XpToNextLevel(guildData.Level))
                    {
                        await LevelUp(guildData);
                    }

                    // Update the cooldown time for the user
                    _lastXpGain[userId] = DateTime.UtcNow;

                    // Save updated user data to the database
                    await _database.UpdateAddUser(userData);
                }
            }
        }

        public async Task LevelUp(GuildData guildData)
        {
            // Use XpToNextLevel to get the XP needed for the current level
            int xpNeededForCurrentLevel = XpToNextLevel(guildData.Level);

            // Increment the user's level
            guildData.Level++;

            // Adjust the user's XP, ensuring it doesn't go below 0
            guildData.Xp = Math.Max(0, guildData.Xp - xpNeededForCurrentLevel);

            // TODO: Add a message to the user that they leveled up maybe even an image
        }
    }

}
