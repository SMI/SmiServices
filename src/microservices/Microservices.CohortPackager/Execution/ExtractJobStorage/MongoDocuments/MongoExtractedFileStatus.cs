
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDocuments
{
    [BsonIgnoreExtraElements]
    public class MongoExtractedFileStatus
    {
        [BsonElement("header")]
        public ExtractFileStatusMessageHeader Header { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("anonymisedFileName")]
        public string AnonymisedFileName { get; set; }

        [BsonElement("statusMessage")]
        public string StatusMessage { get; set; }
    }

    public class ExtractFileStatusMessageHeader
    {
        [BsonElement("fileStatusMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid FileStatusMessageGuid { get; set; }

        [BsonElement("producerIdentifier")]
        public string ProducerIdentifier { get; set; }

        [BsonElement("receivedAt")]
        public DateTime ReceivedAt { get; set; }
    }
}
