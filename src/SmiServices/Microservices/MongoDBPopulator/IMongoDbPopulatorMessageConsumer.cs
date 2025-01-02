using SmiServices.Common.Options;
using SmiServices.Microservices.MongoDBPopulator.Processing;

namespace SmiServices.Microservices.MongoDBPopulator;

public interface IMongoDbPopulatorMessageConsumer
{
    ConsumerOptions ConsumerOptions { get; }

    IMessageProcessor Processor { get; }
}
