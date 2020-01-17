using MongoDB.Bson;
using MongoDB.Driver;

namespace Microservices.IsIdentifiable.Runners
{
    //TODO Move these to a MongoDB reusable module
    public static class MongoHelpers
    {
        private static readonly ListCollectionNamesOptions _listOptions = new ListCollectionNamesOptions();

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
