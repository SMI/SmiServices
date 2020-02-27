using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    public class MongoExtractJob : IEquatable<MongoExtractJob>
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractionJobIdentifier { get; set; }

        [BsonElement("header")]
        public MongoExtractJobHeader Header { get; set; }

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
        public int KeyCount { get; set; }

        [BsonElement("extractionModality")]
        public string ExtractionModality { get; set; }

        [BsonElement("fileCollectionInfo")]
        public List<MongoExpectedFilesForKey> FileCollectionInfo { get; set; }

        public MongoExtractJob() { }

        protected MongoExtractJob(MongoExtractJob existing)
        {
            ExtractionJobIdentifier = existing.ExtractionJobIdentifier;
            Header = existing.Header;
            ProjectNumber = existing.ProjectNumber;
            JobStatus = existing.JobStatus;
            ExtractionDirectory = existing.ExtractionDirectory;
            JobSubmittedAt = existing.JobSubmittedAt;
            KeyTag = existing.KeyTag;
            KeyCount = existing.KeyCount;
            FileCollectionInfo = existing.FileCollectionInfo;
            ExtractionModality = existing.ExtractionModality;
        }

        public bool Equals(MongoExtractJob other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                ExtractionJobIdentifier.Equals(other.ExtractionJobIdentifier) &&
                Header.Equals(other.Header) &&
                string.Equals(ProjectNumber, other.ProjectNumber) &&
                JobStatus == other.JobStatus &&
                string.Equals(ExtractionDirectory, other.ExtractionDirectory) &&
                JobSubmittedAt.Equals(other.JobSubmittedAt) &&
                string.Equals(KeyTag, other.KeyTag) &&
                KeyCount == other.KeyCount &&
                FileCollectionInfo.All(other.FileCollectionInfo.Contains) &&
                ExtractionModality == other.ExtractionModality;
        }
    }

    public class MongoExtractJobHeader : IEquatable<MongoExtractJobHeader>
    {
        [BsonElement("extractRequestInfoMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractRequestInfoMessageGuid { get; set; }

        [BsonElement("producerIdentifier")]
        public string ProducerIdentifier { get; set; }

        [BsonElement("receivedAt")]
        public DateTime ReceivedAt { get; set; }


        public bool Equals(MongoExtractJobHeader other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                ExtractRequestInfoMessageGuid.Equals(other.ExtractRequestInfoMessageGuid) &&
                string.Equals(ProducerIdentifier, other.ProducerIdentifier) &&
                ReceivedAt.Equals(other.ReceivedAt);
        }

        public static MongoExtractJobHeader FromMessageHeader(IMessageHeader header, DateTimeProvider dateTimeProvider)
            => new MongoExtractJobHeader
            {
                ExtractRequestInfoMessageGuid = header.MessageGuid,
                ProducerIdentifier = $"{header.ProducerExecutableName}({header.ProducerProcessID})",
                ReceivedAt = dateTimeProvider.UtcNow(),
            };
    }
}
