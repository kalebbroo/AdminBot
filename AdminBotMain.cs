using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using dotenv.net;
using dotenv.net.Utilities;
using System.IO;

namespace AdminBot
{ 
    class AdminBotMain
    {
        private DiscordSocketClient? _client;

        public static Task Main(string[] args) => new AdminBotMain().MainAsync();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            // Replace this with .env file
            var token = "token";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);

        }
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

    }
}