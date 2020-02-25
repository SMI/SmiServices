using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoExtractJobStore.ObjectModel
{
    //[BsonIgnoreExtraElements]
    public class MongoExtractedFileStatusDocument
    {
        [BsonElement("header")]
        public MongoExtractedFileStatusHeaderDocument Header { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("anonymisedFileName")]
        public string AnonymisedFileName { get; set; }

        [BsonElement("statusMessage")]
        public string StatusMessage { get; set; }
    }

    public class MongoExtractedFileStatusHeaderDocument
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
