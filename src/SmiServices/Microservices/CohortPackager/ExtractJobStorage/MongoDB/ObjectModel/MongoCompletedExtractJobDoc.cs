using MongoDB.Bson.Serialization.Attributes;
using System;


namespace SmiServices.Microservices.CohortPackager.ExtractJobStorage.MongoDB.ObjectModel;

public class MongoCompletedExtractJobDoc : MongoExtractJobDoc
{
    [BsonElement("completedAt")]
    public DateTime CompletedAt { get; set; }

    public MongoCompletedExtractJobDoc(
        MongoExtractJobDoc extractJobDoc,
        DateTime completedAt
    ) : base(extractJobDoc)
    {
        JobStatus = ExtractJobStatus.Completed;
        CompletedAt = completedAt != default ? completedAt : throw new ArgumentException(null, nameof(completedAt));
    }
}
