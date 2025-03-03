using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using AdminBot.Models;
using NetCord.Gateway.Voice;

namespace AdminBot.Services;

/// <summary>
/// Handles voice channel operations and audio streaming following NetCord patterns.
/// Implements voice handling as specified in NetCord's voice documentation.
/// </summary>
public class VoiceService(
    ILogger<VoiceService> logger,
    DatabaseService database,
    ICacheService cache,
    GatewayClient gateway)
{
    private readonly ILogger<VoiceService> _logger = logger;
    private readonly DatabaseService _database = database;
    private readonly ICacheService _cache = cache;
    private readonly GatewayClient _gateway = gateway;
    private readonly Dictionary<ulong, VoiceSession> _activeSessions = new Dictionary<ulong, VoiceSession>();
    private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Joins a voice channel and creates a new session.
    /// </summary>
    public async Task<VoiceSession> JoinChannelAsync(
        ulong guildId,
        ulong channelId,
        ulong userId)
    {
        await _sessionLock.WaitAsync();
        try
        {
            // Check if there's already an active session
            if (_activeSessions.TryGetValue(guildId, out var existingSession))
            {
                if (existingSession.ChannelId == channelId)
                {
                    return existingSession;
                }

                // Clean up old session if moving to new channel
                await LeaveChannelAsync(guildId);
            }

            var session = new VoiceSession
            {
                GuildId = guildId,
                ChannelId = channelId,
                UserId = userId
            };

            try
            {
                // Create voice connection following NetCord pattern
                var voiceClient = await _gateway.JoinVoiceChannelAsync(
                    guildId,
                    channelId);

                await voiceClient.StartAsync();
                session.VoiceClient = voiceClient;
                session.State = AdminBot.Models.VoiceState.Connected;

                // Store session
                _activeSessions[guildId] = session;
                await SaveVoiceSessionAsync(session);

                _logger.LogInformation(
                    "Voice session started in guild {GuildId}, channel {ChannelId}",
                    guildId, channelId);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to join voice channel {ChannelId} in guild {GuildId}",
                    channelId, guildId);
                throw;
            }
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <summary>
    /// Leaves a voice channel and ends the session.
    /// </summary>
    public async Task LeaveChannelAsync(ulong guildId)
    {
        await _sessionLock.WaitAsync();
        try
        {
            if (_activeSessions.TryGetValue(guildId, out var session))
            {
                if (session.VoiceClient != null)
                {
                    session.VoiceClient.Dispose();
                }

                session.EndTime = DateTime.UtcNow;
                session.State = AdminBot.Models.VoiceState.Disconnected;

                await SaveVoiceSessionAsync(session);
                _activeSessions.Remove(guildId);

                _logger.LogInformation(
                    "Voice session ended in guild {GuildId}",
                    guildId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error leaving voice channel in guild {GuildId}",
                guildId);
            throw;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <summary>
    /// Saves a voice session to the database
    /// </summary>
    private async Task SaveVoiceSessionAsync(VoiceSession session)
    {
        try
        {
            _logger.LogInformation("Saving voice session for guild {GuildId}", session.GuildId);
            // Implementation will be completed when voice session database integration is added
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save voice session for guild {GuildId}", session.GuildId);
        }
    }

    /// <summary>
    /// Sends audio to a voice channel from a file, following NetCord's streaming pattern.
    /// </summary>
    public async Task PlayAudioFileAsync(VoiceSession session, string filePath)
    {
        if (session.VoiceClient == null || !session.IsActive)
        {
            throw new InvalidOperationException("No active voice connection");
        }

        try
        {
            // Create FFmpeg process for audio conversion
            using var ffmpeg = CreateFfmpegProcess(filePath);
            using var output = session.VoiceClient.CreateOutputStream();

            // Copy the converted audio to Discord
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(output);
            await output.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error playing audio in guild {GuildId}",
                session.GuildId);
            throw;
        }
    }

    /// <summary>
    /// Sends audio to a voice channel from a stream.
    /// </summary>
    public async Task PlayAudioStreamAsync(VoiceSession session, Stream audioStream)
    {
        if (session.VoiceClient == null || !session.IsActive)
        {
            throw new InvalidOperationException("No active voice connection");
        }

        try
        {
            // Create FFmpeg process for stream conversion
            using var ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });

            // Create output stream to Discord
            using var output = session.VoiceClient.CreateOutputStream();

            // Copy input stream to FFmpeg
            await audioStream.CopyToAsync(ffmpeg.StandardInput.BaseStream);
            ffmpeg.StandardInput.Close();

            // Copy FFmpeg output to Discord
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(output);
            await output.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error streaming audio in guild {GuildId}",
                session.GuildId);
            throw;
        }
    }

    /// <summary>
    /// Creates an FFmpeg process for audio file conversion.
    /// </summary>
    private static Process CreateFfmpegProcess(string inputPath)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{inputPath}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        })!;
    }

    /// <summary>
    /// Gets an active voice session for a guild.
    /// </summary>
    public VoiceSession? GetSession(ulong guildId)
    {
        return _activeSessions.TryGetValue(guildId, out var session) ? session : null;
    }

    /// <summary>
    /// Updates a session's speaking state.
    /// </summary>
    public async Task UpdateSpeakingState(ulong guildId, bool isSpeaking)
    {
        if (_activeSessions.TryGetValue(guildId, out var session))
        {
            session.IsSpeaking = isSpeaking;
            session.UpdateActivity();

            if (isSpeaking)
            {
                await session.VoiceClient!.EnterSpeakingStateAsync(SpeakingFlags.Microphone);
            }
            else
            {
                // Use 0 to indicate not speaking (there is no SpeakingFlags.None)
                await session.VoiceClient!.EnterSpeakingStateAsync((SpeakingFlags)0);
            }
        }
    }

    /// <summary>
    /// Handles cleanup when a user disconnects from voice.
    /// </summary>
    public async Task HandleDisconnect(ulong guildId, ulong userId)
    {
        if (_activeSessions.TryGetValue(guildId, out var session) &&
            session.UserId == userId)
        {
            await LeaveChannelAsync(guildId);
        }
    }

    /// <summary>
    /// Cleanup resources on service disposal.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var guildId in _activeSessions.Keys.ToList())
        {
            await LeaveChannelAsync(guildId);
        }

        _sessionLock.Dispose();
    }
}