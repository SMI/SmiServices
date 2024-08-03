
using Smi.Common.Events;
using Smi.Common.Messages;

namespace Smi.Common.Messaging
{
    // TODO(rkm 2021-04-01) Make this generic over the message type it's expected to send(?)
    /// <summary>
    /// Interface for an object which can send messages to RabbitMQ.
    /// </summary>
    public interface IProducerModel
    {
        /// <summary>
        /// Sends a <see cref="IMessage"/> to a RabbitMQ exchange with the appropriate <see cref="IMessageHeader"/>.
        /// </summary>
        /// <param name="message">Message object to serialise and send.</param>
        /// <param name="isInResponseTo">If you are responding to a message, pass that messages header in here (otherwise pass null)</param>
        /// <param name="routingKey">Routing key for the exchange to direct the message.</param>
        IMessageHeader SendMessage(IMessage message, IMessageHeader? isInResponseTo, string? routingKey);

        /// <summary>
        /// Waits until all sent messages are confirmed by RabbitMQ
        /// </summary>
        void WaitForConfirms();

        /// <summary>
        /// 
        /// </summary>
        event ProducerFatalHandler OnFatal;
    }
}
