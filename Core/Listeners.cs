using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AdminBot.Core
{
    internal class Listeners
    {
        /*
         * private readonly Variables: These lines declare some special variables inside our "Listeners" class.
         * Each one stores an important piece of information or tool that the "Listeners" object will use
         */

        // Think of '_client' as our bot's way to talk and listen to Discord. 
        private readonly DiscordSocketClient _client;
        private readonly Database _database;
        private readonly XpLevels _xpLevels;
        private readonly Random _random = new();

        /* 
         * Special method called a 'constructor'.
         * It's automatically called when a new 'Listeners' object is created.
        */
    public Listeners(DiscordSocketClient client, Database database)
        {
            /*
             * Inside the constructor, There are two parameters: 'client' and 'database'.
             * They are what our 'Listeners' object needs to work properly.
             */

            _client = client; // Here, we're taking the 'client' that was passed in and saving it inside our object. So it can be used later. 
            _database = database; // Similarly, we take the 'database' ans save it inside our object.
            _xpLevels = new XpLevels(client, database); // Lastly, we're creating a new 'XpLevels' object right inside our constructor.
                                                        // We're also passing it the 'client' and 'database'
        }

        // Subscribe to events we want to listen to.
        public void RegisterListeners()
        {
            // Subscribes to the event with our custom methods.
            _client.GuildMemberUpdated += OnGuildMemberUpdatedAsync;
            _client.UserJoined += OnUserJoinedAsync;
            _client.MessageReceived += OnMessageReceivedAsync;
            _client.ReactionAdded += OnReactionAddedAsync;
        }

        private async Task ParseXpForActivity(ulong userId, ulong guildId, int minXP, int maxXP)
        {
            int xpToAdd = _random.Next(minXP, maxXP + 1);
            await _xpLevels.AddXp(userId, guildId, xpToAdd);
            
        }

        // This method is called whenever a user's information changes.
        private async Task OnGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> beforeCacheable, SocketGuildUser after)
        {
            // Attempts to retrieve the 'before' user state from the cache.
            SocketGuildUser before = beforeCacheable.HasValue ? beforeCacheable.Value : null;

            // Checks if we successfully got the 'before' state and if the nickname has changed.
            if (before != null && before.Nickname != after.Nickname)
            {
                // Sends a message to the server's default channel about the nickname change.
                // TODO: Use the DB to get the channel and to save user info.
                ulong guildId = after.Guild.Id;
                ulong userId = after.Id;
                string username = after.Username;
                string beforeNickname = before.Nickname ?? username; // Uses "none" if no nickname was set before.
                string afterNickname = after.Nickname ?? username; // Uses "none" if no nickname is set now.
                var (userData, userCollection) = await _database.GetUserData(userId, guildId, after.Username);
                var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId);
                guildData.DisplayNameChanges++;
                await _database.UpdateAddUser(userData, userCollection);

                var channel = after.Guild.TextChannels.FirstOrDefault(x => x.Name == "bot_spam") ?? after.Guild.DefaultChannel;

                await channel.SendMessageAsync(
                    $"User {username} has changed their nickname from {beforeNickname} to {afterNickname}. " +
                    $"They have changed nicknames {guildData.DisplayNameChanges} times. Their new identity has been secured.");
            }
        }

        // This method is called whenever a user joins the server.
        private async Task OnUserJoinedAsync(SocketGuildUser user)
        {
            // Sends a message to the server's default channel about the user joining.
            // TODO: Create and upload a welcome card to use instead of an embed.
            var embed = new EmbedBuilder()
                .WithTitle("User Joined")
                .WithDescription($"User {user.Mention} has joined the server.")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();

            // TODO: Use the DB to get the channel and to save user info. 
            await user.Guild.DefaultChannel.SendMessageAsync(embed: embed);

            // TODO: Add the user to the DB if they are not already in it.
            ulong userid = user.Id;
            ulong guildid = user.Guild.Id;
            string username = user.Username;
            var (userdata, collection) = await _database.GetUserData(userid, guildid, username);
            await _database.UpdateAddUser(userdata, collection);
        }

        // This method is called whenever a message is received.
        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            // Make sure the message is from a user and not a bot.
            if (message is not SocketUserMessage userMessage || message.Author.IsBot || userMessage.Content.Length < 20)
            {
                return; // Also checks message length
            }
            ulong guildId = (message.Channel as SocketGuildChannel).Guild.Id;
            ulong userId = message.Author.Id;
            await ParseXpForActivity(userId, guildId, 3, 10);
            var (userData, userCollection) = await _database.GetUserData(userId, guildId, message.Author.Username);
            var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId);
            guildData.MessagesSent++;
            await _database.UpdateAddUser(userData, userCollection);
        }

        // This method is called whenever a reaction is added to a message.
        private async Task OnReactionAddedAsync(
            Cacheable<IUserMessage, ulong> cacheableMessage, // This is the message to which the reaction was added. It's "cacheable,"
                                                             // meaning the message data might be stored in memory for quick access.
            Cacheable<IMessageChannel, ulong> cacheableChannel, // This is the channel where the reaction occurred. Also "cacheable,"
            SocketReaction reaction) // Contains information about the reaction itself, like who reacted and with what emoji.
        {
            // Tries to retrieve the channel data from the cache or download it from Discord if it's not already cached.
            var channel = await cacheableChannel.GetOrDownloadAsync();

            // After getting the channel, we cast it to a SocketGuildChannel.
            // The cast is safe to perform because reaction events only occur in guild channels where messages can be reacted to.
            // Casting means we're telling the compiler to treat the IMessageChannel as a SocketGuildChannel.
            await ParseXpForActivity(reaction.UserId, (channel as SocketGuildChannel).Guild.Id,
                1, 5); // Specifies the range of XP to award. In this case, a random amount between 1 and 5 XP will be awarded for adding a reaction.
            ulong guildId = (channel as SocketGuildChannel).Guild.Id;
            ulong userId = reaction.UserId;
            var (userData, userCollection) = await _database.GetUserData(userId, guildId, reaction.User.Value.Username);
            var guildData = userData.Guilds.FirstOrDefault(g => g.GuildId == guildId);
            guildData.ReactionsMade++;
            await _database.UpdateAddUser(userData, userCollection);
        }

    }
}