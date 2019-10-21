
using Microservices.Common.Messaging;
using Microservices.Common.Options;
using Microservices.MongoDBPopulator.Execution.Processing;

namespace Microservices.MongoDBPopulator.Messaging
{
    public interface IMongoDbPopulatorMessageConsumer : IConsumer
    {
        ConsumerOptions ConsumerOptions { get; }

        IMessageProcessor Processor { get; }
    }
}