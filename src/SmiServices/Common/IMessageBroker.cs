using RabbitMQ.Client;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;

namespace SmiServices.Common;

public interface IMessageBroker
{
    bool HasConsumers { get; }

    Guid StartConsumer<T>(ConsumerOptions consumerOptions, IConsumer<T> consumer, bool isSolo) where T : IMessage;

    void StartControlConsumer(IControlMessageConsumer controlMessageConsumer);

    void StopConsumer(Guid taskId, TimeSpan timeout);

    IProducerModel SetupProducer(ProducerOptions producerOptions, bool isBatch);

    IChannel GetModel(string connectionName);

    void Shutdown(TimeSpan timeout);
    public void Wait();

    // Dreams of .NET Core 3.0...
    // void Shutdown() => Shutdown(TimeSpan.FromSeconds(5));
}
