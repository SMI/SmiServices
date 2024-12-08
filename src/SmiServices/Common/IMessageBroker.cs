using RabbitMQ.Client;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;

namespace SmiServices.Common
{
    public interface IMessageBroker
    {
        bool HasConsumers { get; }

        Guid StartConsumer(ConsumerOptions consumerOptions, IConsumer consumer, bool isSolo);

        void StopConsumer(Guid taskId, TimeSpan timeout);

        IProducerModel<T> SetupProducer<T>(ProducerOptions producerOptions, bool isBatch) where T : IMessage;

        IModel GetModel(string connectionName);

        void Shutdown(TimeSpan timeout);
        public void Wait();

        // Dreams of .NET Core 3.0...
        // void Shutdown() => Shutdown(TimeSpan.FromSeconds(5));
    }
}
