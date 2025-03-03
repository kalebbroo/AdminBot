using Microsoft.Extensions.Logging;
using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using AdminBot.Services;
using AdminBot.Utils.Helpers;
using System.Diagnostics;
using OpenAI_API;
using System.ComponentModel.DataAnnotations;
using NetCord.Gateway.Voice;

namespace AdminBot.Modules;

/// <summary>
/// Handles voice commands and TTS functionality following NetCord's voice patterns.
/// Implements voice channel management and audio streaming.
/// </summary>
public enum VoiceType
{
    [SlashCommandChoice("Alloy")]
    Alloy,
    [SlashCommandChoice("Echo")]
    Echo,
    [SlashCommandChoice("Fable")]
    Fable,
    [SlashCommandChoice("Onyx")]
    Onyx,
    [SlashCommandChoice("Nova")]
    Nova
}

public class VoiceCommands(ILogger<VoiceCommands> logger, DatabaseService database, ICacheService cache,
    OpenAIAPI openAi, VoiceService voiceService) : ApplicationCommandModule<SlashCommandContext>
{
    private readonly ILogger<VoiceCommands> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;
    private readonly OpenAIAPI _openAi = openAi;
    private readonly VoiceService _voiceService = voiceService;
    private readonly Dictionary<ulong, VoiceClient> _voiceConnections = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    [SlashCommand("speak", "Generate and play TTS in voice channel")]
    public async Task SpeakAsync(
        [SlashCommandParameter(Description = "The text to speak")]
        string text,
        [SlashCommandParameter(Description = "Voice to use")]
        VoiceType voice = VoiceType.Alloy,
        [SlashCommandParameter(Description = "Speed multiplier (0.25 to 4.0)")]
        [Range(0.25, 4.0)]
        double speed = 1.0)
    {
        // Defer the response while we process the voice command
        InteractionCallback callback = InteractionCallback.DeferredMessage();
        await RespondAsync(callback);

        try
        {
            // Check if user is in a voice channel
            if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ You must be in a voice channel to use TTS.");
                return;
            }

            ulong channelId = voiceState.ChannelId.GetValueOrDefault();
            if (channelId == 0)
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ You must be in a voice channel to use TTS.");
                return;
            }

            // Generate TTS audio
            Stream audioStream = await GenerateTtsAudio(text, voice.ToString().ToLower(), speed);
            if (audioStream == null)
            {
                await Context.Interaction.ModifyResponseAsync(response =>
                    response.Content = "❌ Failed to generate TTS audio.");
                return;
            }

            // Connect to voice channel if not already connected
            var voiceClient = await GetOrCreateVoiceConnection(
                Context.Guild.Id,
                channelId);

            // Play the audio
            await PlayAudioToVoice(voiceClient, audioStream);

            EmbedProperties embed = new DiscordEmbedBuilder()
                .WithTitle("🎙️ TTS Message")
                .WithDescription(text)
                .AddField("Voice", voice.ToString(), true)
                .AddField("Speed", speed.ToString("F1") + "x", true)
                .WithColor(DiscordEmbedBuilder.InfoColor)
                .WithCurrentTimestamp()
                .Build();

            await Context.Interaction.ModifyResponseAsync(response =>
            {
                response.Embeds = [embed];
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TTS command for guild {GuildId}", Context.Guild.Id);
            await Context.Interaction.ModifyResponseAsync(response =>
                response.Content = "❌ An error occurred while processing TTS.");
        }
    }

    private async Task<Stream> GenerateTtsAudio(string text, string voice, double speed)
    {
        try
        {
            // Generate TTS with OpenAI
            Stream response = await _openAi.TextToSpeech.GetSpeechAsStreamAsync(
                new OpenAI_API.Audio.TextToSpeechRequest
                {
                    Input = text,
                    Model = "tts-1",
                    Voice = voice,
                    Speed = (float)speed
                });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TTS audio");
            return null;
        }
    }

    private async Task<VoiceClient> GetOrCreateVoiceConnection(ulong guildId, ulong channelId)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Check if we already have a connection to this guild
            if (_voiceConnections.TryGetValue(guildId, out var existingClient))
            {
                // If we're already in the right channel, return the existing client
                // Note: We need to check the current voice state since VoiceClient doesn't expose ChannelId
                var guild = Context.Guild!;
                if (guild.VoiceStates.TryGetValue(Context.Client.Id, out var botVoiceState) &&
                    botVoiceState.ChannelId.GetValueOrDefault() == channelId)
                {
                    return existingClient;
                }

                // Otherwise, stop the existing client
                existingClient.Abort();
                _voiceConnections.Remove(guildId);
            }

            // Create new connection following NetCord docs
            var voiceClient = await Context.Client.JoinVoiceChannelAsync(
                guildId,
                channelId,
                new VoiceClientConfiguration
                {
                    RedirectInputStreams = false // We don't need to receive voice
                });

            // Connect as shown in docs
            await voiceClient.StartAsync();
            await voiceClient.EnterSpeakingStateAsync(SpeakingFlags.Microphone);

            _voiceConnections[guildId] = voiceClient;
            return voiceClient;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task PlayAudioToVoice(VoiceClient voiceClient, Stream audioStream)
    {
        try
        {
            // Create FFmpeg process to convert audio
            using var ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });

            // Copy audio to FFmpeg
            await audioStream.CopyToAsync(ffmpeg.StandardInput.BaseStream);
            ffmpeg.StandardInput.Close();

            // Create output stream to Discord
            using var output = voiceClient.CreateOutputStream();
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(output);
            await output.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing audio to voice channel");
            throw;
        }
    }

    /// <summary>
    /// Cleanup resources when the module is disposed.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
        foreach (var connection in _voiceConnections.Values)
        {
            connection.Abort();
        }
        _voiceConnections.Clear();
    }
}