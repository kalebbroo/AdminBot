using Discord.Interactions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminBot.Core
{
    public class WarningSystem
    {
        private readonly Database _database;

        // Dependency injection will take care of passing the correct Database instance
        public WarningSystem(Database database)
        {
            _database = database;
        }

        public async Task AddWarningAsync(ulong guildId, ulong userId, string username, string reason, ulong adminId, ulong messageId, string messageContent, string outcome)
        {
            var (userData, usersCollection) = await _database.GetUserData(userId, guildId, username);

            var warning = new Warning
            {
                WarningId = Guid.NewGuid().ToString(),
                AdminId = adminId,
                MessageId = messageId,
                MessageContent = messageContent,
                Reason = reason,
                Date = DateTime.UtcNow,
                Outcome = "Pending"
            };

            var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId) ?? new GuildData { GuildId = guildId };
            if (!userData.Guilds.Contains(guildData))
            {
                userData.Guilds.Add(guildData);
            }
            guildData.Warnings.Add(warning);

            await _database.UpdateAddUser(userData, usersCollection);
        }

        public async Task<List<Warning>> GetWarningsAsync(ulong guildId, ulong userId)
        {
            var (userData, _) = await _database.GetUserData(userId, guildId, null);
            var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId);
            return guildData?.Warnings ?? new List<Warning>();
        }
    }
}
