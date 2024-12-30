using SmiServices.Common.Execution;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;

namespace SmiServices.Microservices.MongoDBPopulator
{
    /// <summary>
    /// Main class to setup and manage the microservice
    /// </summary>
    public class MongoDbPopulatorHost : MicroserviceHost
    {
        public readonly MongoDbPopulatorMessageConsumer<SeriesMessage> SeriesConsumer;
        public readonly MongoDbPopulatorMessageConsumer<DicomFileMessage> ImageConsumer;

        public MongoDbPopulatorHost(GlobalOptions options)
            : base(options)
        {
            SeriesConsumer = new MongoDbPopulatorMessageConsumer<SeriesMessage>(options.MongoDatabases!.DicomStoreOptions!, options.MongoDbPopulatorOptions!, options.MongoDbPopulatorOptions!.SeriesQueueConsumerOptions!);
            ImageConsumer = new MongoDbPopulatorMessageConsumer<DicomFileMessage>(options.MongoDatabases.DicomStoreOptions!, options.MongoDbPopulatorOptions, options.MongoDbPopulatorOptions.ImageQueueConsumerOptions!);
        }

        /// <summary>
        /// Start processing messages
        /// </summary>
        public override void Start()
        {
            Logger.Info("Starting consumers");

            MessageBroker.StartConsumer(SeriesConsumer.ConsumerOptions, SeriesConsumer, isSolo: false);
            MessageBroker.StartConsumer(ImageConsumer.ConsumerOptions, ImageConsumer, isSolo: false);

            Logger.Info("Consumers successfully started");
        }

        /// <summary>
        /// Stop processing messages and shut down
        /// </summary>
        /// <param name="reason"></param>
        public override void Stop(string reason)
        {
            SeriesConsumer.Processor.StopProcessing("Host - " + reason);
            ImageConsumer.Processor.StopProcessing("Host - " + reason);

            base.Stop(reason);
        }
    }
}
