# Adminbot 

## The Main Issues

There were three main issues that caused the TTS feature not to work.
- **Improper dependencies**
- **Premature exiting of the command thread**
- **Invalid audio encoding setup.**


## The Fixes

**Improper Dependencies**

The way this was fixed was by first installing the libopus-dev package inside of the `Dockerfile` the second dependency needed was the Sodium.Core dotnet package. These two dependecies were critical for Discord.NET to be able to send audio. Without these, the app crashes or simply does not send audio.

**Exiting the command thread**

In the original implementation, the audio was requested, recieved, and sent on the command. But this can run into issues because the command threads are not persistent, meaning they will close when the command is done. In our case we want things to be running until the voice memo is done playing. This lead me to implement dependency injection into the bot so that we can access the newly made voiceservice from anywhere.

**Invalid audio encoding setp**

Discord expects a very specific audio format and the openai API was not returning that. So the solution in this case was to first save the audio openai sends us and use FFMPEG to transform the audio into something usable by discord.

