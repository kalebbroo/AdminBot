using Discord;
using Discord.Interactions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;

namespace AdminBot.Core
{
    internal class Database
    {
        private readonly MongoClient client;

        public Database()
        {
            // Retrieve the URI from the .env file
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");

            if (string.IsNullOrEmpty(connectionString)) 
            {
                Console.WriteLine("You must set your 'MONGODB_URI' environment variable.");
                Environment.Exit(0); // Shuts down the bot if the URI is not set
            }
            else
            {
                // Initialize the MongoClient with the URI
                client = new MongoClient(connectionString);
            }

            // Check the connection
            try
            {
                Console.WriteLine("Connecting to MongoDB...");
                client.GetDatabase("AdminBot").RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
                Console.WriteLine("Connected to MongoDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connecting to MongoDB: " + ex.Message);
                Environment.Exit(0); // Shuts down the bot if the connection fails
            }

        }

        // Access the database and collection
        public IMongoCollection<T> GetCollection<T>(string name)
        {
            var database = client.GetDatabase("AdminBot");
            return database.GetCollection<T>(name);
        }

        /* The method to get the user data from the database
         * Takes a userId and returns the user's data from the database
         * This is meant to be the starting point for any user data retrieval
         */
        public async Task<(UserData userData, IMongoCollection<UserData> usersCollection)> GetUserData(ulong userId, ulong guildId)
        {
            // Get the "Users" collection from the database
            var usersCollection = GetCollection<UserData>("Users");
            // Create a filter to find the user by their userId
            var filter = Builders<UserData>.Filter.Eq("userId", userId);
            // Find the user data in the collection
            // Takes usersCollection and filter, finds the first document that matches the filter
            var userData = await usersCollection.Find(filter).FirstOrDefaultAsync();

            if (userData is null)
            {
                Console.WriteLine($"User {userId} not found in the database.");
                // add default user data to the database and return that
                userData = new UserData
                {
                    UserId = userId,
                    Guilds = new List<GuildData>
                    {
                        new GuildData
                        {
                            // set defaulkt values for the user's data in the guild
                            GuildId = guildId,
                            Xp = 0,
                            Level = 1,
                            MessagesSent = 0,
                            DisplayNameChanges = 0,
                            ReactionsMade = 0,
                            Warnings = new List<Warning>()
                        }
                    }
                };
                await UpdateAddUser(userData, usersCollection);
            }
            return (userData, usersCollection);
        }
        // pass mongobd collection to the method and userdata
        public async Task UpdateAddUser(UserData userData, IMongoCollection<UserData> usersCollection)
        {
            var filter = Builders<UserData>.Filter.Eq(u => u.UserId, userData.UserId);

            var updateOptions = new ReplaceOptions { IsUpsert = true };
            await usersCollection.ReplaceOneAsync(filter, userData, updateOptions);

            Console.WriteLine($"User {userData.UserId} updated or added to the database.");
        }
    }

    /*
     * It's generally best practice to define classes that represent the structure of your data. 
     * This is known as creating a model or a Data Transfer Object (DTO). 
     * The reason for doing this is to take advantage of type safety, intellisense, 
     * compile-time checks, and various other features that make your code more maintainable and less prone to errors.   
     * When you work with MongoDB in C#, defining a class that represents your data structure allows the MongoDB C# 
     * driver to map the BSON documents in the database to C# objects. 
     * This object-document mapping (ODM) is done through a process called deserialization.
     */

    /* The UserData class:
     * Represents the global user data. Contains the userId and a list of GuildData objects. 
     * Each GuildData object contains the user's data for each specific guild.
     */
    public class UserData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public ulong UserId { get; set; }

        [BsonElement("guilds")]
        public List<GuildData> Guilds { get; set; }
    }

    /* The GuildData class:
     * Represents the user's data for a specific guild. Contains the guildId and the user's data for that guild.
     * This includes the user's XP, level, messages sent, display name changes, reactions made, and warnings.
     */
    public class GuildData
    {
        [BsonElement("guildId")]
        public ulong GuildId { get; set; }

        [BsonElement("xp")]
        public int Xp { get; set; }

        [BsonElement("level")]
        public int Level { get; set; }

        [BsonElement("messagesSent")]
        public int MessagesSent { get; set; }

        [BsonElement("displayNameChanges")]
        public int DisplayNameChanges { get; set; }

        [BsonElement("reactionsMade")]
        public int ReactionsMade { get; set; }

        [BsonElement("warnings")]
        public List<Warning> Warnings { get; set; }
    }

    /* The Warning class:
     * Represents a warning given to a user. 
     * Contains the warningId, adminId, messageId, messageContent, reason, date, and outcome.
     */
    public class Warning
    {
        [BsonElement("warningId")]
        public string WarningId { get; set; }

        [BsonElement("adminId")]
        public ulong AdminId { get; set; }

        [BsonElement("messageId")]
        public ulong MessageId { get; set; }

        [BsonElement("messageContent")]
        public string MessageContent { get; set; }

        [BsonElement("reason")]
        public string Reason { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("outcome")]
        public string Outcome { get; set; }
    }

    /* Visual representation of the database structure
     * Database: AdminBotDB
│
├───Collection: Users
│   │   Document: User
│   │   {
│   │       "userId": "UniqueUserIdentifier", // Discord User ID
│   │       "guilds": [
│   │           {
│   │               "guildId": "GuildIdentifier1",
│   │               "xp": 120,
│   │               "level": 5,
│   │               "messagesSent": 75,
│   │               "displayNameChanges": 2,
│   │               "reactionsMade": 30,
│   │               "warnings": [
│   │                   {
│   │                       "warningId": "UniqueWarningIdentifier1",
│   │                       "adminId": "AdminUserIdentifier",
│   │                       "messageId": "MessageIdentifier",
│   │                       "messageContent": "Content of the message that led to the warning",
│   │                       "reason": "Reason for the warning",
│   │                       "date": "DateTimeOfWarning",
│   │                       "outcome": "WarningOutcome"
│   │                   }
│   │                   // Other warnings for GuildIdentifier1
│   │               ]
│   │           },
│   │           {
│   │               "guildId": "GuildIdentifier2",
│   │               "xp": 80,
│   │               "level": 3
│   │               // GuildIdentifier2 data, potentially including a "warnings" array as well
│   │           }
│   │       ]
│   │   }
│   │
└───Collection: Guilds
    │   Document: Guild
    │   {
    │       "guildId": "456", // Unique identifier for the guild
    │       "channels": [
    │           {
    │               "channelId": "101", // Unique identifier for the channel
    │               "description": "Rules Channel", // Description or purpose of the channel
    │               "importantMessages": [
    │                   {
    │                       "messageId": "102", // Unique identifier for the message
    │                       "content": "Rule 1: No spamming" // Content of the message
    │                   }
    │                   // More messages can be added here
    │               ]
    │           }
    │           // More channels can be added here
    │       ]
    │   }
     */
}
