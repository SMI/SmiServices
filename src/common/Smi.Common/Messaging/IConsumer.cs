
using Smi.Common.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Smi.Common.Messaging
{
    /// <summary>
    /// Interface for an object which handles messages obtained by a RabbitMQAdapter.
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
    }
}
