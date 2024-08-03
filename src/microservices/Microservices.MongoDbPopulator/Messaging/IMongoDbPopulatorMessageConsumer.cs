
using Microservices.MongoDBPopulator.Execution.Processing;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Microservices.MongoDBPopulator.Messaging
{
    public interface IMongoDbPopulatorMessageConsumer : IConsumer
    {
        ConsumerOptions ConsumerOptions { get; }

        IMessageProcessor Processor { get; }
    }
}
