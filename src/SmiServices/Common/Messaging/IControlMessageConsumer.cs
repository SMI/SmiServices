using SmiServices.Common.Options;

namespace SmiServices.Common.Messaging;

public interface IControlMessageConsumer
{
    ConsumerOptions ControlConsumerOptions { get; }

    void ProcessMessage(string body, string routingKey);
}
