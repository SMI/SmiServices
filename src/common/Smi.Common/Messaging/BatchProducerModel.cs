
using Smi.Common.Messages;
using RabbitMQ.Client;

namespace Smi.Common.Messaging
{
    /// <summary>
    /// Manual confirms - handle yourself
    /// No logging of sent - handle yourself
    /// Make sure to WaitForConfirms during host shutdown
    /// </summary>
    public class BatchProducerModel : ProducerModel
    {
        public BatchProducerModel(string exchangeName, IModel model, IBasicProperties properties, int maxPublishAttempts = 1)
            : base(exchangeName, model, properties, maxPublishAttempts) { }


        /// <summary>
        /// Sends a message but does not wait for the server to confirm the publish. Manually call ProducerModel.WaitForConfirms()
        /// to check all previously unacknowledged messages have been sent.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inResponseTo"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public override IMessageHeader SendMessage(IMessage message, IMessageHeader? inResponseTo = null, string routingKey = "")
        {
            return SendMessageImpl(message, inResponseTo, routingKey);
        }
    }
}
