
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RabbitMQ.Client.Events;
using System;

namespace Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage.MongoDocuments
{
    public class MongoDeadLetterGraveyardDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid MessageGuid { get; set; }

        public MongoDeadLetterDocument DeadLetter { get; set; }

        public DateTime KilledAt { get; set; }

        public string Reason { get; set; }

        public string FullExceptionData { get; set; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="deadLetterDocument"></param>
        /// <param name="reason"></param>
        /// <param name="cause"></param>
        public MongoDeadLetterGraveyardDocument(MongoDeadLetterDocument deadLetterDocument, string reason, Exception cause = null)
        {
            MessageGuid = deadLetterDocument.MessageGuid;
            DeadLetter = deadLetterDocument;
            KilledAt = DateTime.UtcNow;
            Reason = reason;
            if(cause != null)
                FullExceptionData = cause.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deliverArgs"></param>
        /// <param name="messageGuid"></param>
        /// <param name="reason"></param>
        /// <param name="cause"></param>
        public MongoDeadLetterGraveyardDocument(BasicDeliverEventArgs deliverArgs, Guid messageGuid, string reason, Exception cause = null)
            : this(new MongoDeadLetterDocument(deliverArgs, messageGuid, default(DateTime)), reason, cause) { }
    }
}
