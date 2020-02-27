using System;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    public class ArchivedMongoExtractJob : MongoExtractJob, IEquatable<ArchivedMongoExtractJob>
    {
        [BsonElement("archivedAt")]
        public DateTime ArchivedAt { get; set; }

        public ArchivedMongoExtractJob(MongoExtractJob extractJob)
            : base(extractJob) { }

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
