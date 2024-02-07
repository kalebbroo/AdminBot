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
        // TODO: env variables are strings, so we need to parse them to int. Lookup how to do this in C#
        var BaseXp = Environment.GetEnvironmentVariable("BASE_XP");
        var XpMultiplier = Environment.GetEnvironmentVariable("XP_MULTIPLIER");

        public int XpToNextLevel(int level)
        {
            int baseXp = int.Parse(BaseXp ?? "100");
            double xpMultiplier = double.Parse(XpMultiplier ?? "1.5");

            return (int)(baseXp * Math.Pow(xpMultiplier, level - 1));
        }

        public async Task AddXp(ulong userId, ulong guildId, int xpToAdd)
        {
            var userData = await _database.GetUserData(userId, guildId);
            if (userData != null)
            {
                // TODO: Loop through all guilds in the user data and find the guild with the matching guildId
                var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId);
                if (guildData != null)
                {
                    guildData.Xp += xpToAdd;
                    // Check if the XP is enough to level up
                    // if the xp in guildData is greater than or equal to the xp needed to level up
                    if (guildData.Xp >= CalculateXpToNextLevel(guildData.Level))
                    {
                        // Call LevelUp method
                        await LevelUp(guildData);
                    }

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
