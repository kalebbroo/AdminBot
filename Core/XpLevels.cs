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
                    if (guildData.Xp >= XpToNextLevel(guildData.Level))
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
