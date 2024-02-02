using Discord;
using System;

namespace AdminBot.Core
{
    internal class EmbedCreator
    {
        // Use Action Mapping to create the embed
        // This dictionary maps the name of the property to the action that sets the property on the embed.
        // Each key ("title", "description", "url") is mapped to an Action<EmbedBuilder, string>
        // This action takes an EmbedBuilder and a string (the parameter value) and builds the embed.
        // Then when you call CreateEmbed, you can loop through the parameters and call the action for each one.

        // You can call CreateEmbed with a dictionary of parameters like this:
        // var params = new Dictionary<string, string>
        // {
        //     { "title", "My Title" },
        //     { "description", "My Description" },
        //     { "url", "https://example.com" },
        // };
        // var embed = CreateEmbed(params);

        private static readonly Dictionary<string, Action<EmbedBuilder, string>> embedActions = new()
        {
            { "title", (embed, value) => embed.WithTitle(value) },
            { "description", (embed, value) => embed.WithDescription(value) },
            { "url", (embed, value) => embed.WithUrl(value) },
            { "thumbnail", (embed, value) => embed.WithThumbnailUrl(value) },
            { "image", (embed, value) => embed.WithImageUrl(value) },
            { "author", (embed, value) => embed.WithAuthor(value) },
            { "color", (embed, value) => embed.WithColor(new Color(uint.Parse(value, System.Globalization.NumberStyles.HexNumber))) },
            { "footer", (embed, value) => embed.WithFooter(value) },
            { "timestamp", (embed, value) => embed.WithTimestamp(DateTimeOffset.Parse(value)) },
            { "field", (embed, value) => embed.AddField(value.Split('|')[0], value.Split('|')[1]) },
        };
        
        public static Embed CreateEmbed(Dictionary<string, string> parameters)
        {
            // Create a new EmbedBuilder and loop through the parameters
            var embed = new EmbedBuilder();
            foreach (var parameter in parameters) // For each parameter in the dictionary
            {
                if (embedActions.TryGetValue(parameter.Key, out var action)) // If the parameter is in the dictionary
                {
                    embedActions[parameter.Key](embed, parameter.Value); // Call the action with the embed and the parameter value
                }
            }
            return embed.Build();
                   
        }

    }
}
