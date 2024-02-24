using System.Diagnostics;
using Discord.WebSocket;
using System.Threading;
using Discord.Audio;

namespace AdminBot.Core 
{

  /// <summary>
  /// VoiceService handles all the voice channel related tasks of the bot. Audio needs to be sent on its own thread that wont be discarded
  /// </summary>
  public class VoiceService 
  {

    /// <summary>
    /// Dependency injection comes in handy here to give us access to the client instance.
    /// </summary>
    private readonly DiscordSocketClient _client;
    
    public VoiceService(DiscordSocketClient client) {
      _client = client;
    }

    /// <summary>
    /// Creates an ffmpeg process that converts the selected audio file into a discord compatible format.
    /// </summary>
    /// <param name="path">Path of the file you wish to send on discord.</param>
    /// <returns>The ffmpeg process</returns>
    private Process CreateStream(string path)
    {
      return Process.Start(new ProcessStartInfo
      {
        FileName = "ffmpeg",
        Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
        UseShellExecute = false,
        RedirectStandardOutput = true,
      });
    }

    /// <summary>
    /// Send a file through a voice client.
    /// </summary>
    /// <param name="audioClient">The voice client instance</param>
    /// <param name="file">The file you wish to send</param>
    /// <returns></returns>
    public async Task SendVoiceFile(IAudioClient audioClient, string file) {

      try {
        using (var ffmpeg = CreateStream(file))
        using (var output = ffmpeg.StandardOutput.BaseStream)
        using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed)) {
          await output.CopyToAsync(discord);
          await discord.FlushAsync();
        }
      } catch (Exception e) {
        Console.WriteLine(e);
      }

    }

    /// <summary>
    /// Send a stream through a voice client.
    /// </summary>
    /// <param name="audioClient">The voice clietn instance</param>
    /// <param name="stream">The stream you wish to send</param>
    /// <returns></returns>
    public async Task SendVoiceStream(IAudioClient audioClient, Stream stream) {

      using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed)) {
        await stream.CopyToAsync(discord);
        await discord.FlushAsync();
      }

    }

  }

}