using Equ;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using System;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    public class MongoExtractJobDoc : MemberwiseEquatable<MongoExtractJobDoc>
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractionJobIdentifier { get; set; }

        [BsonElement("header")]
        [NotNull]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("projectNumber")]
        [NotNull]
        public string ProjectNumber { get; set; }

        [BsonElement("jobStatus")]
        [BsonRepresentation(BsonType.String)]
        public ExtractJobStatus JobStatus { get; set; }

        [BsonElement("extractionDirectory")]
        [NotNull]
        public string ExtractionDirectory { get; set; }

        [BsonElement("jobSubmittedAt")]
        public DateTime JobSubmittedAt { get; set; }

        [BsonElement("keyTag")]
        [NotNull]
        public string KeyTag { get; set; }

        [BsonElement("keyCount")]
        public uint KeyCount { get; set; }

        [BsonElement("userName")]
        [NotNull]
        public string UserName { get; set; }

        [BsonElement("extractionModality")]
        [CanBeNull]
        public string ExtractionModality { get; set; }

        [BsonElement("isIdentifiableExtraction")]
        public bool IsIdentifiableExtraction { get; set; }

        [BsonElement("IsNoFilterExtraction")]
        public bool IsNoFilterExtraction { get; set; }

        [BsonElement("failedJobInfo")]
        [CanBeNull]
        public MongoFailedJobInfoDoc FailedJobInfoDoc { get; set; }


        public MongoExtractJobDoc(
            Guid extractionJobIdentifier,
            [NotNull] MongoExtractionMessageHeaderDoc header,
            [NotNull] string projectNumber,
            ExtractJobStatus jobStatus,
            [NotNull] string extractionDirectory,
            DateTime jobSubmittedAt,
            [NotNull] string keyTag,
            uint keyCount,
            [NotNull] string userName,
            [CanBeNull] string extractionModality,
            bool isIdentifiableExtraction,
            bool isNoFilterExtraction,
            [CanBeNull] MongoFailedJobInfoDoc failedJobInfoDoc)
        {
            ExtractionJobIdentifier = (extractionJobIdentifier != default(Guid)) ? extractionJobIdentifier : throw new ArgumentException(nameof(extractionJobIdentifier));
            Header = header ?? throw new ArgumentNullException(nameof(header));
            ProjectNumber = (!string.IsNullOrWhiteSpace(projectNumber)) ? projectNumber : throw new ArgumentNullException(nameof(projectNumber));
            JobStatus = (jobStatus != ExtractJobStatus.Unknown) ? jobStatus : throw new ArgumentNullException(nameof(jobStatus));
            ExtractionDirectory = (!string.IsNullOrWhiteSpace(extractionDirectory)) ? extractionDirectory : throw new ArgumentNullException(nameof(extractionDirectory));
            JobSubmittedAt = (jobSubmittedAt != default(DateTime)) ? jobSubmittedAt : throw new ArgumentException(nameof(jobSubmittedAt));
            KeyTag = (!string.IsNullOrWhiteSpace(keyTag)) ? keyTag : throw new ArgumentNullException(nameof(keyTag));
            KeyCount = (keyCount > 0) ? keyCount : throw new ArgumentNullException(nameof(keyCount));
            UserName = (!string.IsNullOrWhiteSpace(userName)) ? userName : throw new ArgumentNullException(nameof(userName));
            if (extractionModality != null)
                ExtractionModality = (!string.IsNullOrWhiteSpace(extractionModality)) ? extractionModality : throw new ArgumentNullException(nameof(extractionModality));
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
            [NotNull] ExtractionRequestInfoMessage message,
            [NotNull] IMessageHeader header,
            [NotNull] DateTimeProvider dateTimeProvider)
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

    public class MongoFailedJobInfoDoc : MemberwiseEquatable<MongoFailedJobInfoDoc>, IEquatable<MongoFailedJobInfoDoc>
    {
        [BsonElement("failedAt")]
        public DateTime FailedAt { get; set; }

        [BsonElement("exceptionMessage")]
        [NotNull]
        public string ExceptionMessage { get; set; }

        [BsonElement("stackTrace")]
        [NotNull]
        public string StackTrace { get; set; }

        [BsonElement("innerException")]
        [CanBeNull]
        public string InnerException { get; set; }


        public MongoFailedJobInfoDoc(
            [NotNull] Exception exception,
            [NotNull] DateTimeProvider dateTimeProvider)
        {
            FailedAt = dateTimeProvider.UtcNow();
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            ExceptionMessage = exception.Message;
            StackTrace = exception.StackTrace;
            InnerException = exception.InnerException?.ToString();
        }
    }
}
