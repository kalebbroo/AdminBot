using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.VisualBasic;
using Discord.Rest;


namespace AdminBot.Core
{
    public class AdminCommands : InteractionModuleBase<SocketInteractionContext>

    {
        // Basic slash implimentation. Still need to add the actual code and the interaction handler
        [SlashCommand("setup_rules", "Sets up the rules for the server")]
        public async Task SetupRules()
        {
            await DeferAsync(ephemeral: true);
            // Define the text channel with the name "rules"
            var RulesChannel = Context.Guild.TextChannels.FirstOrDefault(x => x.Name == "rules");

            if (RulesChannel is null)
            {
                // If the channel does not exist, create it
                await Context.Guild.CreateTextChannelAsync("rules");
            }
            // Send the modal to the user
            await FollowupAsync("test the command", ephemeral: true);

        }

        // The rules modal
        public class RulesModal : IModal
        {
            // Create the modal add each field to make the rules embed
            public string Title => "Create Server Rules";

            [InputLabel("Description")]
            [ModalTextInput("description_input", TextInputStyle.Paragraph,
            placeholder: "Enter a brief description", maxLength: 300)]
            public string? Description { get; set; }
            // Constructors
            public RulesModal() { /* ... */ }
            public RulesModal(string description)
            {
                // Initialize with provided values
                Description = description;
            }
        }

        // Interaction handler for the rules modal
        [ModalInteraction("setup_rules_modal")]
        public async Task OnRulesModalSubmit(RulesModal modal)
        {
            // Add what to do when the modal is submitted
            await DeferAsync();
            await RespondAsync("Rules have been set up!");
        }

        // Add command to test the embed builder
        [SlashCommand("test_embed", "Tests the embed builder")]
        public async Task TestEmbed()
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

        [SlashCommand("ping", "Test command")]
        public async Task Ping()
        {
            await RespondAsync("Pong!", ephemeral: true);
        }

    }
}