
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using SmiServices.Common.Options;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace SmiServices.Common.MongoDB
{
    public static class MongoClientHelpers
    {
        private const string AuthDatabase = "admin"; // Always authenticate against the admin database

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private static readonly ListCollectionNamesOptions _listOptions = new();

        private static readonly ConcurrentDictionary<(MongoDbOptions, string, bool, bool), MongoClient> _clientCache = new();

        /// <summary>
        /// Creates a <see cref="MongoClient"/> from the given options, and checks that the user has the "readWrite" role for the given database
        /// </summary>
        /// <param name="options"></param>
        /// <param name="applicationName"></param>
        /// <param name="skipAuthentication"></param>
        /// <param name="skipJournal"></param>
        /// <returns></returns>
        public static MongoClient GetMongoClient(MongoDbOptions options, string applicationName,
            bool skipAuthentication = false, bool skipJournal = false) =>
            _clientCache.GetOrAdd((options, applicationName, skipAuthentication, skipJournal),
                CreateMongoClient);
        private static MongoClient CreateMongoClient((MongoDbOptions, string, bool, bool) valueTuple)
        {
            var (options, applicationName, skipAuthentication, skipJournal) = valueTuple;

            if (!options.AreValid(skipAuthentication))
                throw new ApplicationException($"Invalid MongoDB options: {options}");

            if (skipAuthentication || options.UserName == string.Empty)
                return new MongoClient(new MongoClientSettings
                {
                    ApplicationName = applicationName,
                    Server = new MongoServerAddress(options.HostName, options.Port),
                    WriteConcern = new WriteConcern(journal: !skipJournal)
                });

            if (string.IsNullOrWhiteSpace(options.Password))
                throw new ApplicationException($"MongoDB password must be set");

            MongoCredential credentials = MongoCredential.CreateCredential(AuthDatabase, options.UserName, options.Password);

            var mongoClientSettings = new MongoClientSettings
            {
                ApplicationName = applicationName,
                Credential = credentials,
                Server = new MongoServerAddress(options.HostName, options.Port),
                WriteConcern = new WriteConcern(journal: !skipJournal),
                DirectConnection = true
            };

            var client = new MongoClient(mongoClientSettings);

            try
            {
                IMongoDatabase db = client.GetDatabase(AuthDatabase);
                var queryResult = db.RunCommand<BsonDocument>(new BsonDocument("usersInfo", options.UserName));

                if (!(queryResult["ok"] == 1))
                    throw new ApplicationException($"Could not check authentication for user \"{options.UserName}\"");

                var roles = (BsonArray)queryResult[0][0]["roles"];

                var hasReadWrite = false;
                foreach (BsonDocument role in roles.Select(x => x.AsBsonDocument))
                    if (role["db"].AsString == options.DatabaseName && role["role"].AsString == "readWrite")
                        hasReadWrite = true;

                if (!hasReadWrite)
                    throw new ApplicationException($"User \"{options.UserName}\" does not have readWrite permissions on database \"{options.DatabaseName}\"");

                _logger.Debug($"User \"{options.UserName}\" successfully authenticated to MongoDB database \"{options.DatabaseName}\"");
            }
            catch (MongoAuthenticationException e)
            {
                throw new ApplicationException($"Could not verify authentication for user \"{options.UserName}\" on database \"{options.DatabaseName}\"", e);
            }

            return client;
        }

        public static IMongoDatabase TryGetDatabase(this MongoClient client, string dbName)
        {
            if (!client.ListDatabaseNames().ToList().Contains(dbName))
                throw new MongoException("Database \'" + dbName + "\' does not exist on the server");

            return client.GetDatabase(dbName);
        }

        public static IMongoCollection<BsonDocument> TryGetCollection(this IMongoDatabase database, string collectionName)
        {
            _listOptions.Filter = new BsonDocument("name", collectionName);

            if (!database.ListCollectionNames(_listOptions).Any())
                throw new MongoException("Collection \'" + collectionName + "\' does not exist in database " + database.DatabaseNamespace);

            return database.GetCollection<BsonDocument>(collectionName);
        }
    }
}
