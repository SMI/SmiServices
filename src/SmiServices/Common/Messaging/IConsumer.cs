using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmiServices.Common.Events;
using SmiServices.Common.Options;

namespace SmiServices.Common.Messaging
{
    /// <summary>
    /// Interface for an object which handles messages obtained by a MessageBroker.
    /// </summary>
    public interface IConsumer
    {
        /// <summary>
        /// Set the <see cref="IChannel"/> which messages will be processed with
        /// </summary>
        /// <param name="model"></param>
        void SetModel(IChannel model);

        /// <summary>
        /// Process a message received by the adapter.
        /// </summary>
        /// <param name="basicDeliverEventArgs">The message and all associated information.</param>
        void ProcessMessage(BasicDeliverEventArgs basicDeliverEventArgs);

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
        /// The BasicQos value configured on the <see cref="IChannel"/>
        /// </summary>
        int QoSPrefetchCount { get; set; }
    }
}
