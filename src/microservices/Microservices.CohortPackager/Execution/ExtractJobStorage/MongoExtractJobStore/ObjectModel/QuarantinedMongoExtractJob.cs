using System;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoExtractJobStore.ObjectModel
{
    public class QuarantinedMongoExtractJob : MongoExtractJob
    {
        [BsonElement("quarantinedAt")]
        public DateTime QuarantinedAt { get; set; }

        [BsonElement("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        [BsonElement("fullExceptionData")]
        public string FullExceptionData { get; set; }


        public QuarantinedMongoExtractJob(MongoExtractJob mongoExtractJob, Exception exception)
            : base(mongoExtractJob)
        {
            QuarantinedAt = DateTime.Now;
            ExceptionMessage = exception.Message;
            FullExceptionData = exception.ToString();
        }
    }
}
