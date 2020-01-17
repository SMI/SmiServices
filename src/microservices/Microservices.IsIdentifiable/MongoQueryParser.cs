using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NLog;

namespace Microservices.IsIdentifiable
{
    public static class MongoQueryParser
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        //TODO(Ruairidh): Refactor out the IMongoCollection object
        public static async Task<IAsyncCursor<BsonDocument>> GetCursor(IMongoCollection<BsonDocument> coll, FindOptions<BsonDocument> findOptions, string jsonQuery)
        {
            if (string.IsNullOrWhiteSpace(jsonQuery))
            {
                _logger.Warn("Not passed a jsonQuery, running an empty find query");
                return await coll.FindAsync(FilterDefinition<BsonDocument>.Empty, findOptions);
            }

            BsonDocument docQuery;

            try
            {
                docQuery = BsonSerializer.Deserialize<BsonDocument>(jsonQuery);
                _logger.Info("Deserialized BsonDocument from string: " + docQuery);
            }
            catch (FormatException e)
            {
                throw new ApplicationException("Could not deserialize the string into a json object", e);
            }

            // Required

            BsonDocument find;
            if (!TryParseDocumentProperty(docQuery, "find", out find))
                throw new ApplicationException("Parsed document did not contain a \"find\" node");

            // Optional

            BsonDocument sort;
            if (TryParseDocumentProperty(docQuery, "sort", out sort))
                findOptions.Sort = sort;

            int limit;
            if (TryParseIntProperty(docQuery, "limit", out limit))
                findOptions.Limit = limit;

            int skip;
            if (TryParseIntProperty(docQuery, "skip", out skip))
                findOptions.Skip = skip;


            return await coll.FindAsync(find, findOptions);
        }

        private static bool TryParseDocumentProperty(BsonDocument docQuery, string propertyName, out BsonDocument propertyDocument)
        {
            BsonValue value;
            if (docQuery.TryGetValue(propertyName, out value))
            {
                try
                {
                    propertyDocument = value.AsBsonDocument;
                    _logger.Info("Parsed document " + propertyDocument + " for property " + propertyName);

                    return true;
                }
                catch (InvalidCastException e)
                {
                    throw new ApplicationException("Could not cast value " + value + " to a document for property " + propertyName, e);
                }
            }

            _logger.Info("No document found for property " + propertyName);
            propertyDocument = null;

            return false;
        }

        private static bool TryParseIntProperty(BsonDocument docQuery, string propertyName, out int propertyValue)
        {
            BsonValue value;
            if (docQuery.TryGetValue(propertyName, out value))
            {
                try
                {
                    propertyValue = value.AsInt32;
                    _logger.Info("Parsed value " + propertyValue + " for property " + propertyName);
                }
                catch (InvalidCastException e)
                {
                    throw new ApplicationException("Could not cast value " + value + " to an int for property " + propertyName, e);
                }

                if (propertyValue < 0)
                    throw new ApplicationException("Property value for " + propertyName + " must be greater than 0");

                return true;
            }

            _logger.Info("No value found for property " + propertyName);
            propertyValue = -1;

            return false;
        }
    }
}
