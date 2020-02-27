using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    /// <summary>
    /// MongoDB document model representing a set of files which are expected to be extracted
    /// </summary>
    public class MongoExpectedFilesForKey : IEquatable<MongoExpectedFilesForKey>
    {
        [BsonElement("header")]
        public ExtractFileCollectionHeader Header { get; set; }

        [BsonElement("key")]
        public string Key { get; set; }

        [BsonElement("expectedFiles")]
        public List<ExpectedAnonymisedFileInfo> AnonymisedFiles { get; set; }

        public bool Equals(MongoExpectedFilesForKey other)
        {
            return other != null &&
                   Header.Equals(other.Header) &&
                   Key == other.Key &&
                   AnonymisedFiles.All(other.AnonymisedFiles.Contains);
        }
    }

    public class ExtractFileCollectionHeader
    {
        [BsonElement("extractFileCollectionInfoMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractFileCollectionInfoMessageGuid { get; set; }

        [BsonElement("producerIdentifier")]
        public string ProducerIdentifier { get; set; }

        [BsonElement("receivedAt")]
        public DateTime ReceivedAt { get; set; }

        public bool Equals(ExtractFileCollectionHeader other)
        {
            return other != null &&
                   ExtractFileCollectionInfoMessageGuid == other.ExtractFileCollectionInfoMessageGuid &&
                   string.Equals(ProducerIdentifier, other.ProducerIdentifier) &&
                   ReceivedAt.Equals(other.ReceivedAt);
        }

        public static ExtractFileCollectionHeader FromMessageHeader(IMessageHeader header, DateTimeProvider dateTimeProvider)
            => new ExtractFileCollectionHeader
            {
                ExtractFileCollectionInfoMessageGuid = header.MessageGuid,
                ProducerIdentifier = $"{header.ProducerExecutableName}({header.ProducerProcessID})",
                ReceivedAt = dateTimeProvider.UtcNow(),
            };
    }

    public class ExpectedAnonymisedFileInfo
    {
        [BsonElement("extractFileMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractFileMessageGuid { get; set; }

        [BsonElement("anonymisedFilePath")]
        public string AnonymisedFilePath { get; set; }
    }
}
