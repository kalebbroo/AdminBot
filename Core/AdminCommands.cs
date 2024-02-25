
using Discord.Interactions;
using System.Diagnostics;
using Discord;
using OpenAI_API;
using OpenAI_API.Audio;
using static OpenAI_API.Audio.TextToSpeechRequest;
using Discord.WebSocket;



namespace AdminBot.Core
{
    public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
    {

        public VoiceService VoiceService { get; set; }
        public WarningSystem WarningSystem { get; set; }

        [SlashCommand("setup_rules", "Sets up the rules for the server")]
        public async Task SetupRules()
        {
            // Define the text channel with the name "rules"
            var RulesChannel = Context.Guild.TextChannels.FirstOrDefault(x => x.Name == "rules");

            if (RulesChannel is null)
            {
                // If the channel does not exist, create it and make that channel the RulesChannel variable
                await Context.Guild.CreateTextChannelAsync("rules");
                RulesChannel = Context.Guild.TextChannels.FirstOrDefault(x => x.Name == "rules");
            }

            // Get the previous message so admins can make small edits instead of retyping the whole thing
            // TODO: Add and retrieve message from DB, Right now it assumes there will only be 1 message in the channel.
            var messages = await RulesChannel.GetMessagesAsync(1).FlattenAsync();
            var message = messages.FirstOrDefault();

            // TODO: Fix this so there is no reused code. Maybe define the default text before the if?
            if (message is null)
            {
                // Create default text to put in the modal. It just looks better..
                string LameTitle = "Server Rules",
                    LameDescription = "No Spamming";
                var rulesModal = new RulesModal(LameTitle, LameDescription);
                // Send the modal to the user
                await RespondWithModalAsync<RulesModal>("setup_rules_modal", rulesModal);
            }
            else
            {
                // Extract the title and description from the embed
                var embed = message.Embeds.FirstOrDefault();
                string LameTitle = embed.Title,
                    LameDescription = embed.Description;
                var rulesModal = new RulesModal(LameTitle, LameDescription);
                // Send the modal to the user
                await RespondWithModalAsync<RulesModal>("setup_rules_modal", rulesModal);
            }

        }

        // The rules modal
        public class RulesModal : IModal
        {
            // Create the modal add each field to make the rules embed
            [InputLabel("Title")]
            [ModalTextInput("title_input", TextInputStyle.Paragraph,
            placeholder: "Enter the title", maxLength: 100)]
            public string? Title { get; set; }

            [InputLabel("Description")]
            [ModalTextInput("description_input", TextInputStyle.Paragraph,
            placeholder: "Enter a brief description", maxLength: 1800)]
            public string? Description { get; set; }
            // Constructors
            public RulesModal() { /* ... */ }
            public RulesModal(string LameTitle, string LameDescription)
            {
                // Initialize with provided values
                Title = LameTitle;
                Description = LameDescription;
            }
        }

        // Interaction handler for the rules modal
        [ModalInteraction("setup_rules_modal")]
        public async Task OnRulesModalSubmit(RulesModal modal)
        {
            await DeferAsync(ephemeral: true);
            // Take the input from the modal and create an embed
            var title = modal.Title;
            var description = modal.Description;

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(Color.Blue)
                .WithCurrentTimestamp()
                .Build();

            // Define the text channel with the name "rules" get from DB in future
            var RulesChannel = Context.Guild.TextChannels.FirstOrDefault(x => x.Name == "rules");
            // Delete the previous message
            var messages = await RulesChannel.GetMessagesAsync(1).FlattenAsync();
            var message = messages.FirstOrDefault();
            if (message is not null)
            {
                await message.DeleteAsync();
            }

            await RulesChannel.SendMessageAsync(embed: embed);
        }

        [SlashCommand("tts", "Text to Speech")]
        public async Task TTS(
        [Summary("text", "The text that will be converted to audio")] string text,
        [Summary("voice", "The voice that will be used to speak")]
        [Choice("Alloy", "alloy"),
         Choice("Echo", "echo"),
         Choice("Fable", "fable"),
         Choice("Onyx", "onyx"),
         Choice("Nova", "nova"),
         Choice("Shimmer", "shimmer")
         ] string voice = "alloy")
        {
            try
            {
                await Context.Interaction.RespondAsync("Creating and sending audio please wait...");
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_KEY");
                Console.WriteLine($"OpenAI API Key: {apiKey}");
                var _openAIClient = new OpenAIAPI(apiKey);
                Console.WriteLine("OpenAI API Key found and initialized");

                var voiceState = (Context.User as IGuildUser)?.VoiceChannel;
                if (voiceState == null)
                {
                    await RespondAsync("You must be in a voice channel to use this command.");
                    return;
                }
                text = string.Join(" ", text);

                var request = new TextToSpeechRequest()
                {
                    Input = text,
                    ResponseFormat = ResponseFormats.MP3,
                    Model = "tts-1", // The model to use for the request. The HD version is slower
                    Voice = voice, // Lets the user choose which voice to use
                    Speed = 0.9
                };

                // First save the TTS file
                await _openAIClient.TextToSpeech.SaveSpeechToFileAsync(request, "tts.mp3");

                // After we are done saving we want to send the file through our voice service which processes it into the right codec.

                // First we create some feedback though
                var resp = await Context.Interaction.GetOriginalResponseAsync();
                await resp.ModifyAsync(x =>
                {
                    x.Content = $"User: **{Context.User.Username}** used TTS to say: \n**{text}** with the **{voice}** voice.";
                });

                // Send Audio
                using (var audioClient = await voiceState.ConnectAsync())
                {

                    await VoiceService.SendVoiceFile(audioClient, "tts.mp3");

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI API connection test failed: {ex.Message}");
                await FollowupAsync("OpenAI API not initialized. Please check the logs for more information.");
                return;
            }
        }

        [SlashCommand("warn", "Give a user a warning or view their warnings")]
        public async Task WarnUser(
        [Summary("user", "Enter the user to list or give warnings")] IUser user,
        [Summary("reason", "Enter the reason for the warning")] string reason = null,
        [Summary("message_id", "The message ID to attach the warning to (optional)")] string messageIdInput = null)
        {
            var guildId = Context.Guild.Id;
            var username = user.Username;
            ulong userId = user.Id;
            ulong adminId = Context.User.Id;
            string messageContent = "Direct warning via command"; // Default message content
            ulong messageId = Context.Interaction.Id; // Default to the interaction ID if no message ID is given

            // Check if the admin provided a specific message ID and fetch its content
            if (!string.IsNullOrEmpty(messageIdInput) && ulong.TryParse(messageIdInput, out ulong parsedMessageId))
            {
                messageId = parsedMessageId;
                var message = await Context.Channel.GetMessageAsync(parsedMessageId) as IUserMessage;
                if (message != null)
                {
                    messageContent = message.Content;
                }
            }

            if (reason == null) // Just get a list of warnings for the specified user
            {
                var warnings = await WarningSystem.GetWarningsAsync(guildId, userId);
                if (warnings.Count == 0)
                {
                    await RespondAsync($"{user.Username} has no warnings.", ephemeral:true);
                    return;
                }

                var warningsEmbed = new EmbedBuilder()
                    .WithTitle($"Displaying Warnings for {user.Username}")
                    .WithColor(Color.DarkRed)
                    .WithFooter(footer => footer.Text = $"Total warnings: {warnings.Count}")
                    .WithCurrentTimestamp();

                // TODO: Handle for the case a user has too many warnings to fit in the embed
                foreach (var warning in warnings)
                {
                    warningsEmbed.AddField($"Warning ID: {warning.WarningId}", $"**Reason:** {warning.Reason}\n**Date:** {warning.Date}\n**Outcome:** {warning.Outcome}");
                }

                await RespondAsync(embed: warningsEmbed.Build());
            }
            else // Add a new warning for the specified user
            {
                await WarningSystem.AddWarningAsync(guildId, userId, username, reason, adminId, messageId, messageContent, "Pending");


                var warningEmbed = new EmbedBuilder()
                    .WithTitle($"New Warning for {user.Username}")
                    //.WithDescription($"{reason}\n\n**Message ID:** {messageId}\n**Message Content:** {messageContent}")
                    .WithDescription($"{reason}\n\n[Message Link](https://discord.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{messageId})\n\n**Message Content:** {messageContent}")
                    .AddField("Admin that issued the warning", Context.User.Username, true)
                    .WithColor(Color.DarkRed)
                    .WithCurrentTimestamp()
                    .Build();

                await RespondAsync(embed: warningEmbed);
            }
        }
    }
}