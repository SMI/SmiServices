
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDocuments
{
    public class MongoExtractJob : IEquatable<MongoExtractJob>
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractionJobIdentifier { get; set; }

        [BsonElement("header")]
        public ExtractJobHeader Header { get; set; }

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
        public List<MongoExtractFileCollection> FileCollectionInfo { get; set; }

        public MongoExtractJob() { }

        //TODO Probably want to implement a deep copy here, but not currently using this in a context where it will matter
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

    public class ExtractJobHeader : IEquatable<ExtractJobHeader>
    {
        [BsonElement("extractRequestInfoMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractRequestInfoMessageGuid { get; set; }

        [BsonElement("producerIdentifier")]
        public string ProducerIdentifier { get; set; }

        [BsonElement("receivedAt")]
        public DateTime ReceivedAt { get; set; }


        public bool Equals(ExtractJobHeader other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                ExtractRequestInfoMessageGuid.Equals(other.ExtractRequestInfoMessageGuid) &&
                string.Equals(ProducerIdentifier, other.ProducerIdentifier) && 
                ReceivedAt.Equals(other.ReceivedAt);
        }
    }
}
