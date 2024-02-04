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

        }

        // Acces the database and collection
        public IMongoCollection<T> GetCollection<T>(string name)
        {
            var database = client.GetDatabase("AdminBot");
            return database.GetCollection<T>(name);
        }

    }
}
