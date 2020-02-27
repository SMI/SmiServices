using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    /// <summary>
    /// MongoDB document model representing a set of files which are expected to be extracted
    /// </summary>
    public class MongoExpectedFilesForKey : IEquatable<MongoExpectedFilesForKey>
    {
        [BsonElement("header")]
        public ExtractFileCollectionHeader Header { get; set; }

        [BsonElement("key")]
        public string Key { get; set; }

        [BsonElement("expectedFiles")]
        public List<ExpectedAnonymisedFileInfo> AnonymisedFiles { get; set; }

        public bool Equals(MongoExpectedFilesForKey other)
        {
            return other != null &&
                   Header.Equals(other.Header) &&
                   Key == other.Key &&
                   AnonymisedFiles.OrderBy(x => x.ExtractFileMessageGuid).SequenceEqual(other.AnonymisedFiles.OrderBy(x => x.ExtractFileMessageGuid));
        }
    }

    public class ExtractFileCollectionHeader
    {
        [BsonElement("extractFileCollectionInfoMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractFileCollectionInfoMessageGuid { get; set; }

        [BsonElement("producerIdentifier")]
        public string ProducerIdentifier { get; set; }

        [BsonElement("receivedAt")]
        public DateTime ReceivedAt { get; set; }

        public static ExtractFileCollectionHeader FromMessageHeader(IMessageHeader header, DateTimeProvider dateTimeProvider)
            => new ExtractFileCollectionHeader
            {
                ExtractFileCollectionInfoMessageGuid = header.MessageGuid,
                ProducerIdentifier = $"{header.ProducerExecutableName}({header.ProducerProcessID})",
                ReceivedAt = dateTimeProvider.UtcNow(),
            };

        #region Equality Members

        protected bool Equals(ExtractFileCollectionHeader other)
        {
            return ExtractFileCollectionInfoMessageGuid.Equals(other.ExtractFileCollectionInfoMessageGuid) &&
                   ProducerIdentifier == other.ProducerIdentifier &&
                   ReceivedAt.Equals(other.ReceivedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractFileCollectionHeader)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ExtractFileCollectionInfoMessageGuid.GetHashCode();
                hashCode = (hashCode * 397) ^ (ProducerIdentifier != null ? ProducerIdentifier.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ReceivedAt.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ExtractFileCollectionHeader left, ExtractFileCollectionHeader right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtractFileCollectionHeader left, ExtractFileCollectionHeader right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
