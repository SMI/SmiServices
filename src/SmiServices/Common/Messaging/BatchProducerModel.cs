using RabbitMQ.Client;
using SmiServices.Common.Messages;
using System;
using System.Threading.Tasks;

namespace SmiServices.Common.Messaging
{
    /// <summary>
    /// Manual confirms - handle yourself
    /// No logging of sent - handle yourself
    /// Make sure to WaitForConfirms during host shutdown
    /// </summary>
    public class BatchProducerModel : ProducerModel
    {
        public BatchProducerModel(
            string exchangeName,
            IChannel model,
            IBasicProperties properties,
            int maxPublishAttempts = 1,
            IBackoffProvider? backoffProvider = null,
            string? probeQueueName = null,
            int probeQueueLimit = 0,
            TimeSpan? probeTimeout = null
        )
            : base(exchangeName, model, properties, maxPublishAttempts, backoffProvider, probeQueueName, probeQueueLimit, probeTimeout)
        { }


        /// <summary>
        /// Sends a message but does not wait for the server to confirm the publish. Manually await the Task
        /// to check all previously unacknowledged messages have been sent.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inResponseTo"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public override Task<IMessageHeader> SendMessage(IMessage message, IMessageHeader? inResponseTo = null,
            string? routingKey = null)
        {
            return SendMessageImpl(message, inResponseTo, routingKey);
        }
    }
}
