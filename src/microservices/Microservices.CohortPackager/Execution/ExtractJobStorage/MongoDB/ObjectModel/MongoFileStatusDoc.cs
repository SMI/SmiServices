using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Messages.Extraction;
using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    [BsonIgnoreExtraElements] // NOTE(rkm 2020-08-28) Required for classes which don't contain a field marked with BsonId
    public class MongoFileStatusDoc : ISupportInitialize
    {
        [BsonElement("header")]
        [NotNull]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("dicomFilePath")]
        [NotNull]
        public string DicomFilePath { get; set; }

        [BsonElement("outputFileName")]
        [CanBeNull]
        public string OutputFileName { get; set; }

        [BsonElement("wasAnonymised")]
        public bool WasAnonymised { get; set; }

        [BsonElement("isIdentifiable")]
        public bool IsIdentifiable { get; set; }

        [BsonElement("extractedFileStatus")]
        [BsonRepresentation(BsonType.String)]
        public ExtractedFileStatus ExtractedFileStatus { get; set; }

        /// <summary>
        /// Should only be null for identifiable extractions where the file was successfully copied. Otherwise will be the failure reason from CTP or the report content from the IsIdentifiable verification
        /// </summary>
        [BsonElement("statusMessage")]
        [CanBeNull]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Used only to handle old-format documents when deserializing
        /// </summary>
        [BsonExtraElements]
        [UsedImplicitly]
        public IDictionary<string, object> ExtraElements { get; set; }


        public MongoFileStatusDoc(
            [NotNull] MongoExtractionMessageHeaderDoc header,
            [NotNull] string dicomFilePath,
            [CanBeNull] string outputFileName,
            bool wasAnonymised,
            bool isIdentifiable,
            ExtractedFileStatus extractedFileStatus,
            [CanBeNull] string statusMessage)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            DicomFilePath = dicomFilePath ?? throw new ArgumentNullException(nameof(dicomFilePath));
            OutputFileName = outputFileName;
            WasAnonymised = wasAnonymised;
            IsIdentifiable = isIdentifiable;
            ExtractedFileStatus = (extractedFileStatus != ExtractedFileStatus.None) ? extractedFileStatus : throw new ArgumentException(nameof(extractedFileStatus));
            StatusMessage = statusMessage;
            if (!IsIdentifiable && string.IsNullOrWhiteSpace(statusMessage))
                throw new ArgumentNullException(nameof(statusMessage));
        }

        // ^ISupportInitialize
        public void BeginInit() { }

        // ^ISupportInitialize
        public void EndInit()
        {
            if (!ExtraElements.ContainsKey("anonymisedFileName"))
                return;

            OutputFileName = (string)ExtraElements["anonymisedFileName"];
            DicomFilePath = "<unknown>";
            ExtractedFileStatus = OutputFileName == null ? ExtractedFileStatus.ErrorWontRetry : ExtractedFileStatus.Anonymised;
        }

        #region Equality Methods

        protected bool Equals(MongoFileStatusDoc other)
        {
            return Equals(Header, other.Header) &&
                   DicomFilePath == other.DicomFilePath &&
                   OutputFileName == other.OutputFileName &&
                   WasAnonymised == other.WasAnonymised &&
                   IsIdentifiable == other.IsIdentifiable &&
                   ExtractedFileStatus == other.ExtractedFileStatus &&
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
                hashCode = (hashCode * 397) ^ (DicomFilePath != null ? DicomFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OutputFileName != null ? OutputFileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (WasAnonymised.GetHashCode());
                hashCode = (hashCode * 397) ^ (IsIdentifiable.GetHashCode());
                hashCode = (hashCode * 397) ^ (ExtractedFileStatus.GetHashCode());
                hashCode = (hashCode * 397) ^ (StatusMessage.GetHashCode());
                return hashCode;
            }
        }

        #endregion
    }
}
