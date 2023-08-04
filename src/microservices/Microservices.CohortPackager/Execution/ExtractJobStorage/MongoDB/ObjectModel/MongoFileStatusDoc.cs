using Equ;
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
    public class MongoFileStatusDoc : MemberwiseEquatable<MongoFileStatusDoc>, ISupportInitialize
    {
        [BsonElement("header")]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("dicomFilePath")]
        public string DicomFilePath { get; set; }

        [BsonElement("outputFileName")]
        public string? OutputFileName { get; set; }

        [BsonElement("extractedFileStatus")]
        [BsonRepresentation(BsonType.String)]
        public ExtractedFileStatus ExtractedFileStatus { get; set; }

        [BsonElement("verifiedFileStatus")]
        [BsonRepresentation(BsonType.String)]
        public VerifiedFileStatus VerifiedFileStatus { get; set; }

        /// <summary>
        /// Should only be null for identifiable extractions where the file was successfully copied. Otherwise will be the failure reason from CTP or the report content from the IsIdentifiable verification
        /// </summary>
        [BsonElement("statusMessage")]
        public string? StatusMessage { get; set; }

        /// <summary>
        /// Used only to handle old-format documents when deserializing
        /// </summary>
        [BsonExtraElements]
        [UsedImplicitly]
        public IDictionary<string, object> ExtraElements { get; set; }


        public MongoFileStatusDoc(
            MongoExtractionMessageHeaderDoc header,
            string dicomFilePath,
            string? outputFileName,
            ExtractedFileStatus extractedFileStatus,
            VerifiedFileStatus verifiedFileStatus,
            string? statusMessage
        )
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            DicomFilePath = dicomFilePath ?? throw new ArgumentNullException(nameof(dicomFilePath));
            OutputFileName = outputFileName;
            ExtractedFileStatus = (extractedFileStatus != ExtractedFileStatus.None) ? extractedFileStatus : throw new ArgumentException("Cannot be None", nameof(extractedFileStatus));
            VerifiedFileStatus = (verifiedFileStatus != VerifiedFileStatus.None) ? verifiedFileStatus : throw new ArgumentException("Cannot be None", nameof(verifiedFileStatus));

            StatusMessage = statusMessage;
            if (string.IsNullOrWhiteSpace(StatusMessage) && ExtractedFileStatus != ExtractedFileStatus.Copied)
                throw new ArgumentException("Cannot be null or whitespace except for successful file copies", nameof(statusMessage));
        }

        // ^ISupportInitialize
        public void BeginInit() { }

        // ^ISupportInitialize
        public void EndInit()
        {
            // NOTE(rkm 2022-07-28) Removed after v1.11.1
            if (ExtraElements.ContainsKey("anonymisedFileName"))
            {
                OutputFileName = (string)ExtraElements["anonymisedFileName"];
                DicomFilePath = "<unknown>";
                ExtractedFileStatus = OutputFileName == null ? ExtractedFileStatus.ErrorWontRetry : ExtractedFileStatus.Anonymised;
            }

            // NOTE(rkm 2022-07-28) Removed after v5.1.3
            if (ExtraElements.ContainsKey("isIdentifiable"))
            {
                if (OutputFileName == null)
                    VerifiedFileStatus = VerifiedFileStatus.NotVerified;
                else
                    VerifiedFileStatus = (bool)ExtraElements["isIdentifiable"] ? VerifiedFileStatus.IsIdentifiable : VerifiedFileStatus.NotIdentifiable;
            }
        }
    }
}
