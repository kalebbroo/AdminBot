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
        // Create a private DiscordSocketClient and InteractionService
        private DiscordSocketClient? _client;
        private InteractionService? _interactions;

        public static Task Main(string[] args) => new AdminBotMain().MainAsync();

        public async Task MainAsync()
        { 
            // Load the .env file from the root directory change the path if needed
            var envFilePath = Path.GetFullPath("../../../.env");
            Console.WriteLine($".env file path:{envFilePath}"); // Debugging

            var envOptions = new DotEnvOptions(envFilePaths: new[] { envFilePath });
            DotEnv.Load(envOptions);

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged
            };

            // Create a new instance of DiscordSocketClient, pass in config.
            _client = new DiscordSocketClient(config);
            // Create a new instance of InteractionService, pass in _client. Needed to listen for interactions.
            _interactions = new InteractionService(_client);
            // Add the Log and ReadyAsync methods to the client's Log and Ready events.
            _client.Log += Log;
            _client.Ready += ReadyAsync;

            // Register command modules with the InteractionService. Tells it to scan AdminBotMain for classes that define slash commands.
            await _interactions.AddModulesAsync(typeof(AdminBotMain).Assembly, null);

            // Obtain the .env variable "BOT_TOKEN". Be sure to create the file locally and add the token in.
            var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

            // Login and start the bot
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
        private async Task ReadyAsync()
        {
            // Things to be run when the bot is ready
            if (_client.Guilds.Any())
            {
                var guildId = _client.Guilds.First().Id;
                await _interactions.AddCommandsToGuildAsync(guildId, true);
                //await _interactions.RegisterCommandsGloballyAsync(true);
            }
            else
            {
                Console.WriteLine($"\nNo guilds found\n");
            }

            Console.WriteLine($"\nLogged in as {_client.CurrentUser.Username}\n" +
                $"Registered {_interactions.ContextCommands.Count} slash commands\n" +
                $"Bot is a member of {_client.Guilds.Count} guilds\n");
            _client.SetGameAsync("/help", null, ActivityType.Listening);
        }

    }
}
