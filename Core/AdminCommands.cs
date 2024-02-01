using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;


namespace AdminBot.Core
{
    public class AdminCommands
    {
        // Basic slash implimentation. Still need to add the actual code and the interaction handler
        [SlashCommand("setup_rules", "Sets up the rules for the server")]
        public async Task SetupRules(InteractionContext context)
        {
            // Define the text channel with the name "rules"
            var channels = await context.Guild.GetChannelsAsync();
            var RulesChannel = channels.FirstOrDefault(channel => channel.Name == "rules");

            if (RulesChannel is null)
            {
                // If the channel does not exist, create it
                await context.Guild.CreateTextChannelAsync("rules");
            }
            // Send the modal to the user

        }

        // The rules modal
        public class RulesModal : IModal
        {
            // Create the modal add each field to make the rules embed
            public string Title => "Create Server Rules";
        }

        // Interaction handler for the rules modal
        [ModalInteraction("setup_rules_modal")]
        public async Task OnRulesModalSubmit(RulesModal modal)
        {
            // Add what to do when the modal is submitted
        }

    }
}