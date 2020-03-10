using System;
using JetBrains.Annotations;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    /// <summary>
    /// 
    /// </summary>
    [BsonIgnoreExtraElements]
    public class MongoFileStatusDoc
    {
        [BsonElement("header")]
        [NotNull]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("anonymisedFileName")]
        [NotNull]
        public string AnonymisedFileName { get; set; }

        [BsonElement("isIdentifiable")]
        public bool IsIdentifiable { get; set; }

        [BsonElement("statusMessage")]
        [NotNull] // NOTE(rkm 2020-02-27) Will be the failure reason from an ExtractFileStatusMessage, or the report content from an IsIdentifiableMessage
        public string StatusMessage { get; set; }

        public MongoFileStatusDoc(
            [NotNull] MongoExtractionMessageHeaderDoc header,
            [NotNull] string anonymisedFileName,
            bool isIdentifiable,
            [NotNull] string statusMessage)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            AnonymisedFileName = anonymisedFileName;
            IsIdentifiable = isIdentifiable;
            StatusMessage = (!string.IsNullOrWhiteSpace(statusMessage)) ? statusMessage : throw new ArgumentNullException(nameof(statusMessage));
        }

        #region Equality Methods

        protected bool Equals(MongoFileStatusDoc other)
        {
            return Equals(Header, other.Header) &&
                   AnonymisedFileName == other.AnonymisedFileName &&
                   IsIdentifiable == other.IsIdentifiable &&
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
                hashCode = (hashCode * 397) ^ (AnonymisedFileName != null ? AnonymisedFileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IsIdentifiable.GetHashCode());
                hashCode = (hashCode * 397) ^ (StatusMessage.GetHashCode());
                return hashCode;
            }
        }

        #endregion
    }
}
