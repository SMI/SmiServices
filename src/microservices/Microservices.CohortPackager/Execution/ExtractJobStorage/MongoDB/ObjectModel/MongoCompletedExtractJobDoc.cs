using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;
using System;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    public class MongoCompletedExtractJobDoc : MongoExtractJobDoc, IEquatable<MongoCompletedExtractJobDoc>
    {
        [BsonElement("completedAt")]
        public DateTime CompletedAt { get; set; }

        public MongoCompletedExtractJobDoc(
            [NotNull] MongoExtractJobDoc extractJobDoc,
            DateTime completedAt
        ) : base(extractJobDoc)
        {
            JobStatus = ExtractJobStatus.Completed;
            CompletedAt = (completedAt != default) ? completedAt : throw new ArgumentException(nameof(completedAt));
        }

        #region Equality Members

        public bool Equals(MongoCompletedExtractJobDoc other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && CompletedAt.Equals(other.CompletedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MongoCompletedExtractJobDoc)obj);
        }

        public static bool operator ==(MongoCompletedExtractJobDoc left, MongoCompletedExtractJobDoc right) => Equals(left, right);

        public static bool operator !=(MongoCompletedExtractJobDoc left, MongoCompletedExtractJobDoc right) => !Equals(left, right);

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ CompletedAt.GetHashCode();
            }
        }

        #endregion
    }
}
