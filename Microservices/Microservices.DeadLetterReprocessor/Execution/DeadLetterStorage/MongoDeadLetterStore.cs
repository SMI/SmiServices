
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microservices.Common.Messages;
using Microservices.Common.Options;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage.MongoDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NLog;
using RabbitMQ.Client.Events;
using Smi.MongoDB.Common;


namespace Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage
{
    public class MongoDeadLetterStore : IDeadLetterStore
    {
        public readonly string DeadLetterStoreCollectionName;
        public readonly string DeadLetterGraveyardCollectionName;

        public const string DeadLetterStoreBaseCollectionName = "deadLetterStore";
        public const string DeadLetterGraveyardBaseCollectionName = "deadLetterGraveyard";


        private readonly ILogger _logger;

        //TODO Can probably get away with collection-level locking if needed
        private readonly IMongoDatabase _database;
        private readonly object _oDbLock = new object();

        private readonly IMongoCollection<MongoDeadLetterDocument> _deadLetterStore;
        private readonly IMongoCollection<MongoDeadLetterGraveyardDocument> _deadLetterGraveyard;

        private readonly FindOptions<MongoDeadLetterDocument> _findOptions = new FindOptions<MongoDeadLetterDocument>
        {
            BatchSize = 1,
            NoCursorTimeout = false
        };


        /// <summary>
        /// Static constructor
        /// </summary>
        static MongoDeadLetterStore()
        {
            var camelCaseConventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConventionPack, type => true);

            BsonClassMap.RegisterClassMap<MessageHeader>(cm =>
            {
                cm.AutoMap();
                cm.MapProperty(x => x.MessageGuid).SetSerializer(new GuidSerializer(BsonType.String));
                cm.MapProperty(x => x.Parents).SetSerializer(new ArraySerializer<Guid>(new GuidSerializer(BsonType.String)));
            });
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="options">Options for connecting to MongoDB</param>
        /// <param name="rabbitMqVirtualHost">RabbitMQ vhost where the messages are located. Used as part of the MongoDB collection names if provided</param>
        public MongoDeadLetterStore(MongoDbOptions options, string rabbitMqVirtualHost = null)
        {
            _logger = LogManager.GetLogger(GetType().Name);

            MongoClient client = MongoClientHelpers.GetMongoClient(options, "DeadLetterReprocessor");
            _database = client.GetDatabase(options.DatabaseName);

            try
            {
                Ping();
            }
            catch (ApplicationException e)
            {
                throw new ArgumentException(
                    "Could not connect to the MongoDB server/database on startup at: " + options.HostName + ":" +
                    options.Port, e);
            }

            var re = new Regex("^[A-Z_]+$", RegexOptions.IgnoreCase);

            if (string.IsNullOrWhiteSpace(rabbitMqVirtualHost) || !re.IsMatch(rabbitMqVirtualHost))
            {
                _logger.Info("Not provided a valid string to label the collections (\"" + rabbitMqVirtualHost + "\"), using the default");

                DeadLetterStoreCollectionName = DeadLetterStoreBaseCollectionName;
                DeadLetterGraveyardCollectionName = DeadLetterGraveyardBaseCollectionName;
            }
            else
            {
                _logger.Info("Provided vhost \"" + rabbitMqVirtualHost + "\" for naming the storage collections");

                DeadLetterStoreCollectionName = DeadLetterStoreBaseCollectionName + "-" + rabbitMqVirtualHost;
                DeadLetterGraveyardCollectionName = DeadLetterGraveyardBaseCollectionName + "-" + rabbitMqVirtualHost;
            }

            _logger.Info("Connecting to dead letter store: " + options.DatabaseName + "." + DeadLetterStoreCollectionName);
            _deadLetterStore = _database.GetCollection<MongoDeadLetterDocument>(DeadLetterStoreCollectionName);
            long count = _deadLetterStore.CountDocuments(FilterDefinition<MongoDeadLetterDocument>.Empty);

            _logger.Info("Connected to " +
                (count > 0
                    ? "dead letter store containing " + count + " existing messages"
                    : "empty dead letter store"));

            _logger.Info("Connecting to dead letter graveyard: " + options.DatabaseName + "." + DeadLetterGraveyardCollectionName);
            _deadLetterGraveyard = _database.GetCollection<MongoDeadLetterGraveyardDocument>(DeadLetterGraveyardCollectionName);
            count = _deadLetterGraveyard.CountDocuments(FilterDefinition<MongoDeadLetterGraveyardDocument>.Empty);

            _logger.Info("Connected to " +
                         (count > 0
                             ? "dead letter graveyard containing " + count + " existing messages"
                             : "empty dead letter graveyard"));
        }

        public void PersistMessageToStore(BasicDeliverEventArgs deliverArgs, IMessageHeader header, TimeSpan retryAfter)
        {
            _logger.Debug("Persisting message " + header.MessageGuid + " to store");

            Guid messageGuid = header.MessageGuid;
            var newDeadLetterDoc = new MongoDeadLetterDocument(deliverArgs, header.MessageGuid, DateTime.UtcNow + retryAfter);

            lock (_oDbLock)
            {
                MongoDeadLetterDocument _;
                if (InDeadLetterStore(messageGuid, out _))
                    throw new ApplicationException("Message already exists in store");

                _deadLetterStore.InsertOne(newDeadLetterDoc);
            }
        }

        public void PersistMessageToStore(IEnumerable<Tuple<BasicDeliverEventArgs, IMessageHeader>> toStore, TimeSpan retryAfter)
        {
            throw new NotImplementedException();
        }

        //TODO This should return an enumerator
        public List<BasicDeliverEventArgs> GetMessagesForReprocessing(string queueFilter, bool forceProcess, Guid messageGuid = new Guid())
        {
            _logger.Debug("Getting messages for republishing (forceProcess=" + forceProcess + ", Guid="
                          + (messageGuid == Guid.Empty ? "<all>" : messageGuid.ToString()) + ")");

            FilterDefinition<MongoDeadLetterDocument> filter = Builders<MongoDeadLetterDocument>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(queueFilter))
                filter &= Builders<MongoDeadLetterDocument>.Filter.Eq(x => x.Props.XDeathHeaders.XFirstDeathQueue, queueFilter);

            if (!forceProcess)
                filter &= Builders<MongoDeadLetterDocument>.Filter.Lt(x => x.RetryAfter, DateTime.UtcNow);

            // If we have been passed a specific GUID, update the filter for that message
            if (messageGuid != default)
                filter &= Builders<MongoDeadLetterDocument>.Filter.Eq(x => x.MessageGuid, messageGuid);

            List<BasicDeliverEventArgs> toReprocess;

            lock (_oDbLock)
                toReprocess = GetMessagesToReprocess(filter).Result;

            _logger.Debug("Found " + toReprocess.Count + " messages to reprocess");

            return toReprocess;
        }

        public void SendToGraveyard(Guid messageGuid, string reason, Exception cause = null)
        {
            _logger.Debug("Sending message " + messageGuid + " to the graveyard (" + reason + ")");

            lock (_oDbLock)
            {
                if (!InDeadLetterStore(messageGuid, out MongoDeadLetterDocument toGraveyard))
                    throw new ApplicationException("Could not find message in store");

                InsertToGraveyard(new MongoDeadLetterGraveyardDocument(toGraveyard, reason, cause));
                DeleteFromStore(messageGuid);
            }
        }

        public void SendToGraveyard(BasicDeliverEventArgs deliverArgs, IMessageHeader header, string reason, Exception cause = null)
        {
            _logger.Debug("Sending message " + header.MessageGuid + " to the graveyard (" + reason + ")");

            var graveyardDoc = new MongoDeadLetterGraveyardDocument(deliverArgs, header.MessageGuid, reason, cause);

            lock (_oDbLock)
            {
                InsertToGraveyard(graveyardDoc);
            }
        }

        public void NotifyMessageRepublished(Guid messageGuid)
        {
            _logger.Debug("Message " + messageGuid + " republished, deleting from store");

            lock (_oDbLock)
            {
                DeleteFromStore(messageGuid);
            }
        }

        #region Helper Methods

        private void Ping(int timeout = 1000)
        {
            bool isLive = _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(timeout);

            if (!isLive)
                throw new ApplicationException("Could not ping MongoDB");
        }

        private static string ToJson<T>(FilterDefinition<T> filter)
        {
            IBsonSerializerRegistry reg = BsonSerializer.SerializerRegistry;
            return filter.Render(reg.GetSerializer<T>(), reg).ToString();
        }

        private bool InDeadLetterStore(Guid messageGuid, out MongoDeadLetterDocument mongoExtractJob)
        {
            mongoExtractJob = _deadLetterStore
                .Find(Builders<MongoDeadLetterDocument>.Filter.Eq(x => x.MessageGuid, messageGuid))
                .SingleOrDefault();

            return mongoExtractJob != null;
        }

        private async Task<List<BasicDeliverEventArgs>> GetMessagesToReprocess(FilterDefinition<MongoDeadLetterDocument> filter)
        {
            _logger.Debug("Getting messages to reprocess with filter " + ToJson(filter));
            var docs = new List<MongoDeadLetterDocument>();

            using (IAsyncCursor<MongoDeadLetterDocument> cursor = await _deadLetterStore.FindAsync(filter, _findOptions))
            {
                while (await cursor.MoveNextAsync())
                    docs.AddRange(cursor.Current);
            }

            _logger.Debug("Received {0} messages to reprocess", docs.Count);

            return docs.Select(x => x.GetBasicDeliverEventArgs()).ToList();
        }

        private void InsertToGraveyard(MongoDeadLetterGraveyardDocument graveyardDoc)
        {
            try
            {
                _deadLetterGraveyard.InsertOne(graveyardDoc);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Exception inserting document into graveyard", e);
            }
        }

        private void DeleteFromStore(Guid messageGuid)
        {
            DeleteResult res = _deadLetterStore.DeleteOne(Builders<MongoDeadLetterDocument>.Filter.Eq(x => x.MessageGuid, messageGuid));

            if (!(res.IsAcknowledged || res.DeletedCount != 1))
                throw new Exception("Message could not be deleted from the store");
        }

        #endregion
    }
}
