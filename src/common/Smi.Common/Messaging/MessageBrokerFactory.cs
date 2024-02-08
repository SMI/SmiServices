using System;
using Smi.Common.Options;

namespace Smi.Common.Messaging;

public static class MessageBrokerFactory
{
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
                throw new ArgumentOutOfRangeException(nameof(globals.MessageBrokerType), $"A valid {nameof(MessageBrokerType)} must be chosen");
            default:
                throw new NotImplementedException($"No case for {globals.MessageBrokerType}");
        }
    }
}
