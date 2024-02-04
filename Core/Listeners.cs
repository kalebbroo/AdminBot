using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AdminBot.Core
{
    internal class Listeners
    {
        // Holds a reference to the Discord client to interact with the Discord API.
        private readonly DiscordSocketClient _client;

        // Constructor that takes the Discord client as a parameter.
        // This allows the Listeners class to use the client passed from elsewhere in the bot.
        public Listeners(DiscordSocketClient client)
        {
            _client = client; // Saves the passed client for use in this class.
        }

        // Subscribe to events we want to listen to.
        public void RegisterListeners()
        {
            // Subscribes to the GuildMemberUpdated event with our custom OnGuildMemberUpdatedAsync method.
            _client.GuildMemberUpdated += OnGuildMemberUpdatedAsync;

        }

        // This method is called whenever a user's information changes.
        private async Task OnGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> beforeCacheable, SocketGuildUser after)
        {
            // Attempts to retrieve the 'before' user state from the cache.
            SocketGuildUser before = beforeCacheable.HasValue ? beforeCacheable.Value : null;

            // Checks if we successfully got the 'before' state and if the nickname has changed.
            if (before != null && before.Nickname != after.Nickname)
            {
                // TODO: Maybe use username instead of none if no nickname is set?
                string beforeNickname = before.Nickname ?? "none"; // Uses "none" if no nickname was set before.
                string afterNickname = after.Nickname ?? "none"; // Uses "none" if no nickname is set now.

                // Sends a message to the server's default channel about the nickname change.
                // TODO: Use the DB to get the channel and to save user info.
                await after.Guild.DefaultChannel.SendMessageAsync($"User {after.Mention} has changed their nickname from {beforeNickname} to {afterNickname}.");
            }
        }
    }
}