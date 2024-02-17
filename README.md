## AdminBot
A Discord admin bot written in C#

This Discord bot is designed to provide various functionalities including:
	- XP and levels system 
		- //TODO: Rank and Leaderboard commands
	- Moderation warning system //TODO
	- Mongodb integration
	- Custom commands
		- TTS (Text to speech) using OpenAI's API
		- Rules command to setup a rules message

## Prerequisites

Before you can run this bot, you need to have the following prerequisites set up:

1. **MongoDB:** A running MongoDB instance for the bot's database requirements. You can use a local MongoDB server or a MongoDB Atlas cluster. (Included in Docker installation)

2. **Discord Bot:** A Discord bot created through the Discord Developer Portal. You will need the bot token for authentication.

3. **OpenAI API Key:** An API key from OpenAI for accessing the text-to-speech service. You need to sign up at OpenAI and obtain an API key.
 
4. **Docker:** If you want to run the bot in a Docker container, you need to have Docker installed on your system.

5. **.NET 8.0 SDK:** If you want to run the bot directly on your system, you need to have the .NET 8.0 SDK installed. (Included in Docker installation)

## Installation

The recommended way to install and run this bot is using Docker. This method ensures that the bot runs in a contained environment with all its dependencies.

### Setting up Environment Variables

Create a `.env` file in the root directory of the project with the following environment variables:

```env
BOT_TOKEN=<Your Discord Bot Token>
MONGODB_URI=<Your MongoDB URI> # If you are using Docker, Use the default URI (mongodb://mongo:27017/adminbot) or change it to your needs.
OPENAI_KEY=<Your OpenAI API Key>
```

Replace `Your Discord Bot Token`, `Your MongoDB URI`, and `Your OpenAI API Key` with your actual Discord bot token, MongoDB URI, and OpenAI API key respectively.

## Running with Docker
Open the Docker folder run the 1-click install script. This will build the Docker image and run the bot in a Docker container.

## Contributing
Contributions to the bot are welcome. Please ensure to follow the project's coding standards and submit a pull request for any enhancements.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.
