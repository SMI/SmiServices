using System;
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    public class MongoFileStatusDoc
    {
        [BsonElement("header")]
        [NotNull]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("status")]
        [CanBeNull] // NOTE(rkm 2020-03-04) Will be null for messages received from the validation tool
        public string Status { get; set; }

        [BsonElement("anonymisedFileName")]
        [CanBeNull] // NOTE(rkm 2020-03-04) Will be null for messages we receive regarding failed anonymisation
        public string AnonymisedFileName { get; set; }

        [BsonElement("statusMessage")]
        [NotNull] // NOTE(rkm 2020-02-27) Will be the failure reason from an ExtractFileStatusMessage, or the report content from an IsIdentifiableMessage
        public string StatusMessage { get; set; }

        public MongoFileStatusDoc(
            [NotNull] MongoExtractionMessageHeaderDoc header,
            [CanBeNull] string status,
            [CanBeNull] string anonymisedFileName,
            [NotNull] string statusMessage)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            if (status != null)
                Status = (!string.IsNullOrWhiteSpace(status)) ? status : throw new ArgumentNullException(nameof(status));
            if (anonymisedFileName != null)
                AnonymisedFileName = (!string.IsNullOrWhiteSpace(anonymisedFileName)) ? anonymisedFileName : throw new ArgumentNullException(nameof(anonymisedFileName));
            StatusMessage = (!string.IsNullOrWhiteSpace(statusMessage)) ? statusMessage : throw new ArgumentNullException(nameof(statusMessage));
        }

        #region Equality Methods

        protected bool Equals(MongoFileStatusDoc other)
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
            if (obj.GetType() != GetType()) return false;
            return Equals((MongoFileStatusDoc)obj);
        }

        public static bool operator ==(MongoFileStatusDoc left, MongoFileStatusDoc right) => Equals(left, right);

        public static bool operator !=(MongoFileStatusDoc left, MongoFileStatusDoc right) => !Equals(left, right);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Header.GetHashCode());
                hashCode = (hashCode * 397) ^ (Status != null ? Status.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AnonymisedFileName != null ? AnonymisedFileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StatusMessage.GetHashCode());
                return hashCode;
            }
        }

        #endregion
    }
}
