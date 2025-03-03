using System;
using System.Text;
using NetCord;
using NetCord.Rest;

namespace AdminBot.Utils.Helpers;

/// <summary>
/// Helper class for building embeds with consistent styling and validation.
/// </summary>
public class DiscordEmbedBuilder
{
    // Standard brand colors for different message types
    public static readonly Color SuccessColor = new(46, 204, 113);   // Green
    public static readonly Color ErrorColor = new(231, 76, 60);      // Red
    public static readonly Color InfoColor = new(52, 152, 219);      // Blue
    public static readonly Color WarningColor = new(241, 196, 15);   // Yellow
    public static readonly Color DefaultColor = new(88, 101, 242);   // Discord Blurple

    // Discord limits
    private const int MaxEmbedLength = 6000;
    private const int MaxDescriptionLength = 4096;
    private const int MaxFieldLength = 1024;
    private const int MaxTitleLength = 256;
    private const int MaxFieldCount = 25;

    private readonly EmbedProperties _properties;
    private int _totalLength;

    public DiscordEmbedBuilder()
    {
        _properties = new EmbedProperties();
        _totalLength = 0;
    }

    /// <summary>
    /// Creates a standard success embed.
    /// </summary>
    public static EmbedProperties Success(string title, string description)
    {
        return new DiscordEmbedBuilder()
            .WithTitle("✅ " + title)
            .WithDescription(description)
            .WithColor(SuccessColor)
            .WithCurrentTimestamp()
            .Build();
    }

    /// <summary>
    /// Creates a standard error embed.
    /// </summary>
    public static EmbedProperties Error(string title, string description, Exception? exception = null)
    {
        var builder = new DiscordEmbedBuilder()
            .WithTitle("❌ " + title)
            .WithDescription(description)
            .WithColor(ErrorColor)
            .WithCurrentTimestamp();

        if (exception != null)
        {
            builder.AddField("Error Details", $"```{exception.Message}```");
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a standard info embed.
    /// </summary>
    public static EmbedProperties Info(string title, string description)
    {
        return new DiscordEmbedBuilder()
            .WithTitle("ℹ️ " + title)
            .WithDescription(description)
            .WithColor(InfoColor)
            .WithCurrentTimestamp()
            .Build();
    }

    /// <summary>
    /// Creates a standard warning embed.
    /// </summary>
    public static EmbedProperties Warning(string title, string description)
    {
        return new DiscordEmbedBuilder()
            .WithTitle("⚠️ " + title)
            .WithDescription(description)
            .WithColor(WarningColor)
            .WithCurrentTimestamp()
            .Build();
    }

    /// <summary>
    /// Sets the title of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithTitle(string title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            if (title.Length > MaxTitleLength)
                title = title[..MaxTitleLength];

            _properties.Title = title;
            _totalLength += title.Length;
        }
        return this;
    }

    /// <summary>
    /// Sets the description of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithDescription(string description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            if (description.Length > MaxDescriptionLength)
                description = description[..MaxDescriptionLength];

            _properties.Description = description;
            _totalLength += description.Length;
        }
        return this;
    }

    /// <summary>
    /// Sets the color of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithColor(Color color)
    {
        _properties.Color = color;
        return this;
    }

    /// <summary>
    /// Sets the author of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithAuthor(string name, string? url = null, string? iconUrl = null)
    {
        _properties.Author = new EmbedAuthorProperties
        {
            Name = name,
            Url = url,
            IconUrl = iconUrl
        };
        _totalLength += name.Length;
        return this;
    }

    /// <summary>
    /// Sets the thumbnail of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithThumbnail(string url)
    {
        _properties.Thumbnail = url;
        return this;
    }

    /// <summary>
    /// Sets the image of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithImage(string url)
    {
        _properties.Image = url;
        return this;
    }

    /// <summary>
    /// Sets the footer of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithFooter(string text, string? iconUrl = null)
    {
        _properties.Footer = new EmbedFooterProperties
        {
            Text = text,
            IconUrl = iconUrl
        };
        _totalLength += text.Length;
        return this;
    }

    /// <summary>
    /// Sets the timestamp to the current time.
    /// </summary>
    public DiscordEmbedBuilder WithCurrentTimestamp()
    {
        _properties.Timestamp = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Sets the timestamp of the embed.
    /// </summary>
    public DiscordEmbedBuilder WithTimestamp(DateTimeOffset timestamp)
    {
        _properties.Timestamp = timestamp;
        return this;
    }

    /// <summary>
    /// Adds a field to the embed.
    /// </summary>
    public DiscordEmbedBuilder AddField(string name, string value, bool inline = false)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            return this;

        if (name.Length > MaxFieldLength)
            name = name[..MaxFieldLength];

        if (value.Length > MaxFieldLength)
            value = value[..MaxFieldLength];

        var field = new EmbedFieldProperties
        {
            Name = name,
            Value = value,
            Inline = inline
        };

        var fields = _properties.Fields?.ToList() ?? new List<EmbedFieldProperties>();
        fields.Add(field);
        _properties.Fields = fields.ToArray();

        _totalLength += name.Length + value.Length;
        return this;
    }

    /// <summary>
    /// Adds a progress bar visualization.
    /// </summary>
    public DiscordEmbedBuilder AddProgressBar(double progress, int length = 20)
    {
        var filledLength = (int)(length * Math.Min(1, Math.Max(0, progress)));
        var emptyLength = length - filledLength;

        _properties.Description += $"\n{new string('█', filledLength)}{new string('░', emptyLength)} {progress:P0}";
        return this;
    }

    /// <summary>
    /// Adds a code block to the description.
    /// </summary>
    public DiscordEmbedBuilder AddCodeBlock(string content, string? language = null)
    {
        _properties.Description += $"\n```{language ?? ""}\n{content}\n```";
        return this;
    }

    /// <summary>
    /// Builds and returns the embed.
    /// </summary>
    public EmbedProperties Build()
    {
        if (_totalLength > MaxEmbedLength)
            throw new InvalidOperationException($"Embed content exceeds maximum length of {MaxEmbedLength} characters");

        return _properties;
    }
}