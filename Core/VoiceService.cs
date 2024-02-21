using System.Diagnostics;
using Discord.WebSocket;
using System.Threading;
using Discord.Audio;

namespace AdminBot.Core 
{

  public class VoiceService 
  {

    private readonly DiscordSocketClient _client;
    
    public VoiceService(DiscordSocketClient client) {
      _client = client;
    }

    private Process CreateFfmpeg() {
      return new Process {
          StartInfo =  {
            FileName = "ffmpeg",
            Arguments = " -hide_banner -loglevel panic -i -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardInput = true
          }
      };
    }

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

    public async Task SendVoiceFile(IAudioClient audioClient, string file) {

      using (var ffmpeg = CreateStream(file))
      using (var output = ffmpeg.StandardOutput.BaseStream)
      using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed)) {
          await output.CopyToAsync(discord);
          await discord.FlushAsync();
      }

    }

    public async Task SendVoiceStream(IAudioClient audioClient, Stream stream) {

      using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed)) {
          await stream.CopyToAsync(discord);
          await discord.FlushAsync();
      }

    }

  }

}