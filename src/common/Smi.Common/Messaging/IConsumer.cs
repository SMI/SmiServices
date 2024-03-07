
using Smi.Common.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Smi.Common.Options;

namespace Smi.Common.Messaging
{
    /// <summary>
    /// Interface for an object which handles messages obtained by a MessageBroker.
    /// </summary>
    public interface IConsumer
    {
        /// <summary>
        /// Set the <see cref="IModel"/> which messages will be processed with
        /// </summary>
        /// <param name="model"></param> 
        void SetModel(IModel model);

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
        /// The BasicQos value configured on the <see cref="IModel"/>
        /// </summary>
        int QoSPrefetchCount { get; set; }
    }
}
