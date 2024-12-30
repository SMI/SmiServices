using RabbitMQ.Client;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;

namespace SmiServices.Common.Messaging
{
    /// <summary>
    /// Interface for an object which handles messages obtained by a MessageBroker.
    /// </summary>
    public interface IConsumer<T> where T : IMessage
    {
        /// <summary>
        /// Process a message received by the adapter.
        /// </summary>
        void ProcessMessage(IMessageHeader header, T message, ulong tag);

        /// <summary>
        /// Callback raised when Ack-ing a message
        /// </summary>
        event AckEventHandler OnAck;

        /// <summary>
        /// Callback raised when Nack-ing a message
        /// </summary>
        event NackEventHandler OnNack;

        /// <summary>
        /// 
        /// </summary>
        event ConsumerFatalHandler? OnFatal;

        /// <summary>
        /// Trigger a clean shutdown of worker threads etc
        /// </summary>
        void Shutdown();

        /// <summary>
        /// If set, consumer will not call Fatal when an unhandled exception occurs when processing a message. Requires <see cref="ConsumerOptions.AutoAck"/> to be false
        /// </summary>
        bool HoldUnprocessableMessages { get; set; }

        /// <summary>
        /// The BasicQos value configured on the <see cref="IModel"/>
        /// </summary>
        int QoSPrefetchCount { get; set; }
    }
}
