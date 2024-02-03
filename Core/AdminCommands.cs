using Discord.Interactions;
using Discord;


namespace AdminBot.Core
{
    public class AdminCommands : InteractionModuleBase<SocketInteractionContext>

    {
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

        // Add command to test the embed builder
        [SlashCommand("test_embed", "Tests the embed builder")]
        public async Task TestEmbed()
        {
            try
            {
                // Create a new embed
                var parameters = new Dictionary<string, string>
                {
                    { "title", "My Title" },
                    { "description", "My Description" },
                    { "url", "https://example.com" },
                    { "thumbnail", "https://example.com/thumbnail.png" },
                    { "image", "https://example.com/image.png" },
                    { "author", "My Author" },
                    { "color", "FF5733" },
                    { "footer", "My Footer" },
                    { "timestamp", "2022-01-01T00:00:00+00:00" },
                    { "field", "Field 1|Value 1" },
                    { "field", "Field 2|Value 2" },
                };
                var embed = EmbedCreator.CreateEmbed(parameters);

                // Send the embed
                await RespondAsync(embed: embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [SlashCommand("ping", "Test command")]
        public async Task Ping()
        {
            await RespondAsync("Pong!", ephemeral: true);
        }

    }
}