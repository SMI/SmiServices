using MongoDB.Bson;
using MongoDB.Driver;
using NLog;
using SmiServices.Common.MongoDB;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;


namespace SmiServices.Microservices.MongoDBPopulator
{
    /// <summary>
    /// Class to handle the MongoDb connection
    /// </summary>
    public class MongoDbAdapter : IMongoDbAdapter
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IMongoDatabase _database;
        private readonly string _defaultCollectionName;
        private readonly IMongoCollection<BsonDocument> _defaultCollection;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="applicationName">Name to identify the connection to MongoDb with</param>
        /// <param name="mongoDbOptions"></param>
        /// <param name="defaultCollectionName">Default collectionNamePostfix to write to unless overridden</param>
        public MongoDbAdapter(string applicationName, MongoDbOptions mongoDbOptions, string defaultCollectionName)
        {
            if (string.IsNullOrWhiteSpace(defaultCollectionName))
                throw new ArgumentException(null, nameof(defaultCollectionName));

            _logger.Debug("MongoDbAdapter: Creating connection to MongoDb on " + mongoDbOptions.HostName + ":" + mongoDbOptions.Port);

            //TODO Standardise AppId
            MongoClient mongoClient = MongoClientHelpers.GetMongoClient(mongoDbOptions, "MongoDbPopulator::" + applicationName, string.IsNullOrWhiteSpace(mongoDbOptions.UserName));

            _logger.Debug("MongoDbAdapter: Getting reference to database " + mongoDbOptions.DatabaseName);
            _database = mongoClient.GetDatabase(mongoDbOptions.DatabaseName);

            _logger.Debug("MongoDbAdapter: Getting reference to collection " + defaultCollectionName);
            _defaultCollectionName = defaultCollectionName;
            _defaultCollection = _database.GetCollection<BsonDocument>(defaultCollectionName);

            _logger.Debug("MongoDbAdapter: Checking initial collection");

            bool isLive = _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);

            if (!isLive)
                throw new ArgumentException($"Could not connect to the MongoDB server/database on startup at {mongoDbOptions.HostName}:{mongoDbOptions.Port}");

            _logger.Debug("MongoDbAdapter: Connection setup successfully");
        }


        /// <summary>
        /// Writes one or more <see cref="BsonDocument"/>(s) to MongoDb
        /// </summary>
        /// <param name="toWrite"></param>
        /// <param name="collectionNamePostfix">Optional argument to write to a different collection with the specified tag</param>
        /// <returns></returns>
        public WriteResult WriteMany(IList<BsonDocument> toWrite, string? collectionNamePostfix = null)
        {
            if (!toWrite.Any())
                return WriteResult.Success;

            //TODO Test whether pre-fetching references to all the image_* collections results in any speedup
            IMongoCollection<BsonDocument> collectionForWrite =
                collectionNamePostfix == null
                    ? _defaultCollection
                    : _database.GetCollection<BsonDocument>($"{_defaultCollectionName}_{collectionNamePostfix}");

            _logger.Info($"Attempting bulk write of {toWrite.Count} documents to {collectionForWrite.CollectionNamespace}");

            try
            {
                //TODO Try and determine if any write errors are to do with a document in the batch or not
                BulkWriteResult<BsonDocument> res = collectionForWrite.BulkWrite(toWrite.Select(d => new InsertOneModel<BsonDocument>(d)));
                _logger.Debug(" Write to {0} acknowledged: {1}", collectionForWrite.CollectionNamespace, res.IsAcknowledged);
                return res.IsAcknowledged ? WriteResult.Success : WriteResult.Failure;
            }
            catch (MongoBulkWriteException e)
            {
                //TODO Determine possible causes of MongoBulkWriteException
                _logger.Error("Exception when writing to MongoDb: " + e);
                return WriteResult.Unknown;
            }
        }
    }
}
