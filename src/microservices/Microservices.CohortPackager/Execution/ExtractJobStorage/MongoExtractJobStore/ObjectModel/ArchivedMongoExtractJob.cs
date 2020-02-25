using System;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoExtractJobStore.ObjectModel
{
    public class ArchivedMongoExtractJob : MongoExtractJob, IEquatable<ArchivedMongoExtractJob>
    {
        [BsonElement("archivedAt")]
        public DateTime ArchivedAt { get; set; }
        

        public ArchivedMongoExtractJob(MongoExtractJob mongoExtractJob, DateTime archivedAt)
            : base(mongoExtractJob)
        {
            ArchivedAt = archivedAt;
        }
        

        public bool Equals(ArchivedMongoExtractJob other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                base.Equals(other) &&
                ArchivedAt.Equals(other.ArchivedAt);
        }
    }
}
