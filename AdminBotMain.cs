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
            var envFilePath = Path.GetFullPath("../../../.env");

            var envOptions = new DotEnvOptions(envFilePaths: new[] { envFilePath });
            DotEnv.Load(envOptions);

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All
            };

            _client = new DiscordSocketClient(config);
            _client.Log += Log;

            // Obtain the .env variable "BOT_TOKEN". Be sure to create the file locally and add the token in.
            var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);

        }
        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

    }
}
