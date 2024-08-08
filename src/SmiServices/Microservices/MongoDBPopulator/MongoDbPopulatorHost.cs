using SmiServices.Common.Execution;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmiServices.Microservices.MongoDBPopulator
{
    /// <summary>
    /// Main class to setup and manage the microservice
    /// </summary>
    public class MongoDbPopulatorHost : MicroserviceHost
    {
        public readonly List<IMongoDbPopulatorMessageConsumer> Consumers = [];


        public MongoDbPopulatorHost(GlobalOptions options)
            : base(options)
        {
            Consumers.Add(new MongoDbPopulatorMessageConsumer<SeriesMessage>(options.MongoDatabases!.DicomStoreOptions!, options.MongoDbPopulatorOptions!, options.MongoDbPopulatorOptions!.SeriesQueueConsumerOptions!));
            Consumers.Add(new MongoDbPopulatorMessageConsumer<DicomFileMessage>(options.MongoDatabases.DicomStoreOptions!, options.MongoDbPopulatorOptions, options.MongoDbPopulatorOptions.ImageQueueConsumerOptions!));

            if (Consumers.Count == 0)
                throw new ArgumentException("No consumers created from the given options");
        }

        /// <summary>
        /// Start processing messages
        /// </summary>
        public override void Start()
        {
            Logger.Info("Starting consumers");

            foreach (IMongoDbPopulatorMessageConsumer consumer in Consumers)
                MessageBroker.StartConsumer(consumer.ConsumerOptions, consumer, isSolo: false);

            Logger.Info("Consumers successfully started");
        }

        /// <summary>
        /// Stop processing messages and shut down
        /// </summary>
        /// <param name="reason"></param>
        public override void Stop(string reason)
        {
            foreach (IMongoDbPopulatorMessageConsumer consumer in Consumers)
                consumer.Processor.StopProcessing("Host - " + reason);

            base.Stop(reason);
        }
    }
}
