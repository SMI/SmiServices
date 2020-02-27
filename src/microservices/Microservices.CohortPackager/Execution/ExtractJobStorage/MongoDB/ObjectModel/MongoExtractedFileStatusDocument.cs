using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    //[BsonIgnoreExtraElements]
    public class MongoExtractedFileStatusDocument
    {
        [BsonElement("header")]
        public MongoExtractedFileStatusHeaderDocument Header { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        // NOTE(rkm 2020-02-27) Will be null for failed anonymisation
        [BsonElement("anonymisedFileName")]
        public string AnonymisedFileName { get; set; }

        //  NOTE(rkm 2020-02-27) Will be the failure reason from an ExtractFileStatusMessage, or the report content from an IsIdentifiableMessage
        [BsonElement("statusMessage")]
        public string StatusMessage { get; set; }

        #region Equality Methods

        protected bool Equals(MongoExtractedFileStatusDocument other)
        {
            return Equals(Header, other.Header) &&
                   Status == other.Status &&
                   AnonymisedFileName == other.AnonymisedFileName &&
                   StatusMessage == other.StatusMessage;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MongoExtractedFileStatusDocument)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Header != null ? Header.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Status != null ? Status.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AnonymisedFileName != null ? AnonymisedFileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StatusMessage != null ? StatusMessage.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(MongoExtractedFileStatusDocument left, MongoExtractedFileStatusDocument right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MongoExtractedFileStatusDocument left, MongoExtractedFileStatusDocument right)
        {
            return !Equals(left, right);
        }

        #endregion
    }

    public class MongoExtractedFileStatusHeaderDocument
    {
        [BsonElement("fileStatusMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid FileStatusMessageGuid { get; set; }

        [BsonElement("producerIdentifier")]
        public string ProducerIdentifier { get; set; }

        [BsonElement("receivedAt")]
        public DateTime ReceivedAt { get; set; }

        public static MongoExtractedFileStatusHeaderDocument FromMessageHeader(IMessageHeader header, DateTimeProvider dateTimeProvider)
            => new MongoExtractedFileStatusHeaderDocument
            {
                FileStatusMessageGuid = header.MessageGuid,
                ProducerIdentifier = $"{header.ProducerExecutableName}({header.ProducerProcessID})",
                ReceivedAt = dateTimeProvider.UtcNow(),
            };
        
        #region Equality Methods

        protected bool Equals(MongoExtractedFileStatusHeaderDocument other)
        {
            return FileStatusMessageGuid.Equals(other.FileStatusMessageGuid) &&
                   ProducerIdentifier == other.ProducerIdentifier &&
                   ReceivedAt.Equals(other.ReceivedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MongoExtractedFileStatusHeaderDocument)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = FileStatusMessageGuid.GetHashCode();
                hashCode = (hashCode * 397) ^ (ProducerIdentifier != null ? ProducerIdentifier.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ReceivedAt.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(MongoExtractedFileStatusHeaderDocument left, MongoExtractedFileStatusHeaderDocument right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MongoExtractedFileStatusHeaderDocument left, MongoExtractedFileStatusHeaderDocument right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
