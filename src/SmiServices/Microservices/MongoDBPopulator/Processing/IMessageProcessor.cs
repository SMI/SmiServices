using RabbitMQ.Client;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;

namespace SmiServices.Microservices.MongoDBPopulator.Processing;

/// <summary>
/// Interface for classes which process <see cref="IMessage"/>(s) into MongoDb
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMessageProcessor<in T> : IMessageProcessor where T : IMessage
{
    /// <summary>
    /// Add a message to the write queue
    /// </summary>
    /// <param name="message">Message to write</param>
    /// <param name="header"></param>
    /// <param name="deliveryTag">Delivery tag for the message acknowledgement</param>
    void AddToWriteQueue(T message, IMessageHeader header, ulong deliveryTag);
}

public interface IMessageProcessor
{
    /// <summary>
    /// Indicates if the processor is stopping and no more messages should be queued for processing
    /// </summary>
    bool IsStopping { get; }

    event SmiAckEventHandler? OnAck;

    /// <summary>
    /// Count of the total number of acknowledged messages during this processors lifetime
    /// </summary>
    int AckCount { get; }

    /// <summary>
    /// Stops the continuous processing of messages
    /// </summary>
    void StopProcessing(string reason);
}
