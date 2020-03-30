using System;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    /// <summary>
    /// Class which represents a document created from an extraction message header
    /// </summary>
    public class MongoExtractionMessageHeaderDoc : IEquatable<MongoExtractionMessageHeaderDoc>
    {
        [BsonElement("extractionJobIdentifier")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractionJobIdentifier { get; set; }

        [BsonElement("messageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid MessageGuid { get; set; }

        [BsonElement("producerExecutableName")]
        [NotNull]
        public string ProducerExecutableName { get; set; }

        [BsonElement("producerProcessID")]
        public int ProducerProcessID { get; set; }

        [BsonElement("originalPublishTimestamp")]
        public DateTime OriginalPublishTimestamp { get; set; }

        [BsonElement("parents")]
        [CanBeNull]
        public string Parents { get; set; }

        [BsonElement("receivedAt")]
        public DateTime ReceivedAt { get; set; }


        public MongoExtractionMessageHeaderDoc(
            Guid extractionJobIdentifier,
            Guid messageGuid,
            [NotNull] string producerExecutableName,
            int producerProcessId,
            DateTime originalPublishTimestamp,
            [CanBeNull] string parents,
            DateTime receivedAt)
        {
            ExtractionJobIdentifier = (extractionJobIdentifier != default(Guid)) ? extractionJobIdentifier : throw new ArgumentNullException(nameof(extractionJobIdentifier));
            MessageGuid = (messageGuid != default(Guid)) ? messageGuid : throw new ArgumentNullException(nameof(messageGuid));
            ProducerExecutableName = (!string.IsNullOrWhiteSpace(producerExecutableName)) ? producerExecutableName : throw new ArgumentNullException(nameof(producerExecutableName));
            ProducerProcessID = (producerProcessId > 0) ? producerProcessId : throw new ArgumentNullException(nameof(producerProcessId));
            OriginalPublishTimestamp = (originalPublishTimestamp != default(DateTime)) ? originalPublishTimestamp : throw new ArgumentNullException(nameof(originalPublishTimestamp));
            Parents = parents;
            ReceivedAt = (receivedAt != default(DateTime)) ? receivedAt : throw new ArgumentNullException(nameof(receivedAt));
        }

        public static MongoExtractionMessageHeaderDoc FromMessageHeader(
            Guid extractionJobIdentifier,
            [NotNull] IMessageHeader header,
            [NotNull] DateTimeProvider dateTimeProvider)
        {
            return new MongoExtractionMessageHeaderDoc(
                extractionJobIdentifier,
                header.MessageGuid,
                header.ProducerExecutableName,
                header.ProducerProcessID,
                MessageHeader.UnixTimeToDateTime(header.OriginalPublishTimestamp),
                string.Join(MessageHeader.Splitter, header.Parents),
                dateTimeProvider.UtcNow()
            );
        }

        #region Equality Members

        public bool Equals(MongoExtractionMessageHeaderDoc other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ExtractionJobIdentifier.Equals(other.ExtractionJobIdentifier) &&
                   MessageGuid.Equals(other.MessageGuid) &&
                   ProducerExecutableName == other.ProducerExecutableName &&
                   ProducerProcessID == other.ProducerProcessID &&
                   OriginalPublishTimestamp.Equals(other.OriginalPublishTimestamp) &&
                   Parents == other.Parents &&
                   ReceivedAt.Equals(other.ReceivedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MongoExtractionMessageHeaderDoc)obj);
        }

        public static bool operator ==(MongoExtractionMessageHeaderDoc left, MongoExtractionMessageHeaderDoc right) => Equals(left, right);

        public static bool operator !=(MongoExtractionMessageHeaderDoc left, MongoExtractionMessageHeaderDoc right) => !Equals(left, right);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = MessageGuid.GetHashCode();
                hashCode = (hashCode * 397) ^ ExtractionJobIdentifier.GetHashCode();
                hashCode = (hashCode * 397) ^ ProducerExecutableName.GetHashCode();
                hashCode = (hashCode * 397) ^ ProducerProcessID;
                hashCode = (hashCode * 397) ^ OriginalPublishTimestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ (Parents != null ? Parents.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ReceivedAt.GetHashCode();
                return hashCode;
            }
        }

        #endregion
    }
}
