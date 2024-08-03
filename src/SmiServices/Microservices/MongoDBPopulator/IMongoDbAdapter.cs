
using System.Collections.Generic;
using MongoDB.Bson;

namespace SmiServices.Microservices.MongoDBPopulator
{
    /// <summary>
    /// Possible return statuses of a write operation
    /// </summary>
    public enum WriteResult { Success, Failure, Unknown }

    public interface IMongoDbAdapter
    {
        WriteResult WriteMany(IList<BsonDocument> toWrite, string? collectionNamePostfix = null);
    }
}
