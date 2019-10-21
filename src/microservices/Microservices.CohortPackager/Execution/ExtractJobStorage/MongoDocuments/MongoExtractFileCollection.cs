
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDocuments
{
    public class MongoExtractFileCollection : IEquatable<MongoExtractFileCollection>
    {
        [BsonElement("header")]
        public ExtractFileCollectionHeader Header { get; set; }

        [BsonElement("keyValue")]
        public string KeyValue { get; set; }

        [BsonElement("expectedFiles")]
        public List<ExpectedAnonymisedFileInfo> AnonymisedFiles { get; set; }
        

        public bool Equals(MongoExtractFileCollection other)
        {
            return other != null &&
                   Header.Equals(other.Header) &&
                   KeyValue == other.KeyValue &&
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
