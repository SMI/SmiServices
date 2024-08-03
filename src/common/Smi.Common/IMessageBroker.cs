using System;
using RabbitMQ.Client;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Smi.Common
{
    public interface IMessageBroker
    {
        bool HasConsumers { get; }

        Guid StartConsumer(ConsumerOptions consumerOptions, IConsumer consumer, bool isSolo);

        void StopConsumer(Guid taskId, TimeSpan timeout);

        IProducerModel SetupProducer(ProducerOptions producerOptions, bool isBatch);

        IModel GetModel(string connectionName);

        void Shutdown(TimeSpan timeout);
        public void Wait();

        // Dreams of .NET Core 3.0...
        // void Shutdown() => Shutdown(TimeSpan.FromSeconds(5));
    }
}
