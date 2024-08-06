using Equ;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using System;


namespace SmiServices.Microservices.CohortPackager.ExtractJobStorage.MongoDB.ObjectModel
{
    public class MongoExtractJobDoc : MemberwiseEquatable<MongoExtractJobDoc>
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractionJobIdentifier { get; set; }

        [BsonElement("header")]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("projectNumber")]
        public string ProjectNumber { get; set; }

        [BsonElement("jobStatus")]
        [BsonRepresentation(BsonType.String)]
        public ExtractJobStatus JobStatus { get; set; }

        [BsonElement("extractionDirectory")]
        public string ExtractionDirectory { get; set; }

        [BsonElement("jobSubmittedAt")]
        public DateTime JobSubmittedAt { get; set; }

        [BsonElement("keyTag")]
        public string KeyTag { get; set; }

        [BsonElement("keyCount")]
        public uint KeyCount { get; set; }

        [BsonElement("userName")]
        public string UserName { get; set; }

        [BsonElement("extractionModality")]
        public string? ExtractionModality { get; set; }

        [BsonElement("isIdentifiableExtraction")]
        public bool IsIdentifiableExtraction { get; set; }

        [BsonElement("IsNoFilterExtraction")]
        public bool IsNoFilterExtraction { get; set; }

        [BsonElement("failedJobInfo")]
        public MongoFailedJobInfoDoc? FailedJobInfoDoc { get; set; }


        public MongoExtractJobDoc(
            Guid extractionJobIdentifier,
            MongoExtractionMessageHeaderDoc header,
            string projectNumber,
            ExtractJobStatus jobStatus,
            string extractionDirectory,
            DateTime jobSubmittedAt,
            string keyTag,
            uint keyCount,
            string userName,
            string? extractionModality,
            bool isIdentifiableExtraction,
            bool isNoFilterExtraction,
            MongoFailedJobInfoDoc? failedJobInfoDoc)
        {
            ExtractionJobIdentifier = extractionJobIdentifier != default ? extractionJobIdentifier : throw new ArgumentException(nameof(extractionJobIdentifier));
            Header = header ?? throw new ArgumentNullException(nameof(header));
            ProjectNumber = !string.IsNullOrWhiteSpace(projectNumber) ? projectNumber : throw new ArgumentNullException(nameof(projectNumber));
            JobStatus = jobStatus != ExtractJobStatus.Unknown ? jobStatus : throw new ArgumentNullException(nameof(jobStatus));
            ExtractionDirectory = !string.IsNullOrWhiteSpace(extractionDirectory) ? extractionDirectory : throw new ArgumentNullException(nameof(extractionDirectory));
            JobSubmittedAt = jobSubmittedAt != default ? jobSubmittedAt : throw new ArgumentException(nameof(jobSubmittedAt));
            KeyTag = !string.IsNullOrWhiteSpace(keyTag) ? keyTag : throw new ArgumentNullException(nameof(keyTag));
            KeyCount = keyCount > 0 ? keyCount : throw new ArgumentNullException(nameof(keyCount));
            UserName = !string.IsNullOrWhiteSpace(userName) ? userName : throw new ArgumentNullException(nameof(userName));
            if (extractionModality != null)
                ExtractionModality = !string.IsNullOrWhiteSpace(extractionModality) ? extractionModality : throw new ArgumentNullException(nameof(extractionModality));
            IsIdentifiableExtraction = isIdentifiableExtraction;
            IsNoFilterExtraction = isNoFilterExtraction;
            FailedJobInfoDoc = failedJobInfoDoc;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public MongoExtractJobDoc(MongoExtractJobDoc existing)
        {
            ExtractionJobIdentifier = existing.ExtractionJobIdentifier;
            Header = existing.Header;
            ProjectNumber = existing.ProjectNumber;
            JobStatus = existing.JobStatus;
            ExtractionDirectory = existing.ExtractionDirectory;
            JobSubmittedAt = existing.JobSubmittedAt;
            KeyTag = existing.KeyTag;
            KeyCount = existing.KeyCount;
            UserName = existing.UserName;
            ExtractionModality = existing.ExtractionModality;
            IsIdentifiableExtraction = existing.IsIdentifiableExtraction;
            FailedJobInfoDoc = existing.FailedJobInfoDoc;
            IsNoFilterExtraction = existing.IsNoFilterExtraction;
        }

        public static MongoExtractJobDoc FromMessage(
            ExtractionRequestInfoMessage message,
            IMessageHeader header,
            DateTimeProvider dateTimeProvider)
        {
            return new MongoExtractJobDoc(
                message.ExtractionJobIdentifier,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(message.ExtractionJobIdentifier, header, dateTimeProvider),
                message.ProjectNumber,
                ExtractJobStatus.WaitingForCollectionInfo,
                message.ExtractionDirectory,
                message.JobSubmittedAt,
                message.KeyTag,
                (uint)message.KeyValueCount,
                message.UserName,
                message.ExtractionModality,
                message.IsIdentifiableExtraction,
                message.IsNoFilterExtraction,
                null
            );
        }
    }

    public class MongoFailedJobInfoDoc : MemberwiseEquatable<MongoFailedJobInfoDoc?>, IEquatable<MongoFailedJobInfoDoc>
    {
        [BsonElement("failedAt")]
        public DateTime FailedAt { get; set; }

        [BsonElement("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        [BsonElement("stackTrace")]
        public string StackTrace { get; set; }

        [BsonElement("innerException")]
        public string? InnerException { get; set; }

        public MongoFailedJobInfoDoc(
            Exception exception,
            DateTimeProvider dateTimeProvider
        )
        {
            FailedAt = dateTimeProvider.UtcNow();
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            ExceptionMessage = exception.Message;
            StackTrace = exception.StackTrace!;
            InnerException = exception.InnerException?.ToString();
        }
    }
}
