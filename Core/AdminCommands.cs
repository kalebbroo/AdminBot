using Discord;
using Discord_Slash_Commands;

namespace AdminBot.AdminCommands
{
    public class AdminCommands
    {
        // Basic slash implimentation. Still need to add the actual code and the interaction handler
        [SlashCommand("setup_rules", "Sets up the rules for the server")]
        public async Task SetupRules(InteractionContext ctx)
        {
            // This command will open a modal for the user to input the rules
            // When the moadl is submitted it should create an embed with the rules
            // It should then save the rules to a database
        }
    }
}