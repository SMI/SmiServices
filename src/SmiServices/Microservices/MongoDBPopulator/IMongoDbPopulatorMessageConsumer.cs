using Smi.Common.Messaging;
using Smi.Common.Options;
using SmiServices.Microservices.MongoDBPopulator.Processing;

namespace SmiServices.Microservices.MongoDBPopulator
{
    public interface IMongoDbPopulatorMessageConsumer : IConsumer
    {
        ConsumerOptions ConsumerOptions { get; }

        IMessageProcessor Processor { get; }
    }
}
