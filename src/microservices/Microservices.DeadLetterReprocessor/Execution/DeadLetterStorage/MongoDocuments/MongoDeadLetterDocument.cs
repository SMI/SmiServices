
using Smi.Common.Messages;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage.MongoDocuments
{
    public class MongoDeadLetterDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid MessageGuid { get; set; }

        public DateTime RetryAfter { get; set; }

        public string RoutingKey { get; set; }
        
        public MongoBasicPropertiesDocument Props { get; set; }

        public string Payload { get; set; }


        public MongoDeadLetterDocument(BasicDeliverEventArgs deliverArgs, Guid messageGuid, DateTime retryAfter)
        {
            MessageGuid = messageGuid;
            RetryAfter = retryAfter;
            RoutingKey = deliverArgs.RoutingKey;
            Props = new MongoBasicPropertiesDocument(deliverArgs.BasicProperties);
            Payload = Encoding.UTF8.GetString(deliverArgs.Body.ToArray());
        }


        public BasicDeliverEventArgs GetBasicDeliverEventArgs()
        {
            return new BasicDeliverEventArgs
            {
                RoutingKey = RoutingKey,
                Body = Encoding.UTF8.GetBytes(Payload)
            };
        }
    }

    public class MongoBasicPropertiesDocument
    {
        public string ContentEncoding { get; set; }

        public string ContentType { get; set; }

        public MessageHeader MessageHeader { get; set; }

        public RabbitMqXDeathHeaders XDeathHeaders { get; set; }

        public Dictionary<string,object> Headers { get; set;  }

        public long UnixTimestamp { get; set; }

        public MongoBasicPropertiesDocument(IBasicProperties props)
        {
            Headers = new Dictionary<string,object>(props.Headers);
            foreach (string key in new string[] { "MessageGuid", "ProducerProcessID", "ProducerExecutableName","Parents","OriginalPublishTimestamp" }) {
                Headers.Remove(key);
            }
            foreach (string key in RabbitMqXDeathHeaders._requiredKeys)
            {
                Headers.Remove(key);
            }
            ContentEncoding = props.ContentEncoding;
            ContentType = props.ContentType;
            MessageHeader = new MessageHeader(props.Headers, Encoding.UTF8);
            XDeathHeaders = new RabbitMqXDeathHeaders(props.Headers, Encoding.UTF8);
            UnixTimestamp = props.Timestamp.UnixTime;
        }

    }
}
