using SmiServices.Common.Options;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Common.Messaging;

public static class MessageBrokerFactory
{
    [ExcludeFromCodeCoverage] // NOTE(rkm 2024-02-08) This can be removed after we use the class
    public static IMessageBroker Create(GlobalOptions globals, string connectionIdentifier)
    {
        switch (globals.MessageBrokerType)
        {
            case MessageBrokerType.RabbitMQ:
                {
                    if (globals.RabbitOptions == null)
                        throw new ArgumentNullException(nameof(globals), $"{nameof(globals.RabbitOptions)} must not be null");

                    return new RabbitMQBroker(globals.RabbitOptions, connectionIdentifier);
                }
            case MessageBrokerType.None:
                throw new ArgumentOutOfRangeException(nameof(globals), $"A valid {nameof(MessageBrokerType)} must be chosen");
            default:
                throw new NotImplementedException($"No case for {globals.MessageBrokerType}");
        }
    }
}
