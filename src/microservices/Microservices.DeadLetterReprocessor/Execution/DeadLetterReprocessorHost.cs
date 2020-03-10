
using Smi.Common.Execution;
using Smi.Common.Options;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterRepublishing;
using Microservices.DeadLetterReprocessor.Execution.DeadLetterStorage;
using Microservices.DeadLetterReprocessor.Messaging;
using Microservices.DeadLetterReprocessor.Options;
using System;
using System.Threading;

namespace Microservices.DeadLetterReprocessor.Execution
{
    public class DeadLetterReprocessorHost : MicroserviceHost
    {
        private readonly DeadLetterRepublisher _deadLetterRepublisher;
        private readonly DeadLetterQueueConsumer _deadLetterQueueConsumer;

        private readonly DeadLetterReprocessorCliOptions _cliOptions;

        public DeadLetterReprocessorHost(GlobalOptions globals, DeadLetterReprocessorCliOptions cliOptions, bool loadSmiLogConfig = true)
            : base(globals, loadSmiLogConfig: loadSmiLogConfig)
        {
            var deadLetterStore = new MongoDeadLetterStore(globals.MongoDatabases.DeadLetterStoreOptions, Globals.RabbitOptions.RabbitMqVirtualHost);

            _deadLetterQueueConsumer = new DeadLetterQueueConsumer(deadLetterStore, globals.DeadLetterReprocessorOptions);
            _deadLetterRepublisher = new DeadLetterRepublisher(deadLetterStore, RabbitMqAdapter.GetModel("DeadLetterRepublisher"));

            _cliOptions = cliOptions;
        }

        public override void Start()
        {
            Logger.Info("Starting dead letter consumer");

            Guid consumerId = RabbitMqAdapter.StartConsumer(Globals.DeadLetterReprocessorOptions.DeadLetterConsumerOptions, _deadLetterQueueConsumer, true);

            do
            {
                Thread.Sleep(1000);
            } while (_deadLetterQueueConsumer.MessagesInQueue());
                

            Logger.Info("DLQ empty, stopping consumer");
            RabbitMqAdapter.StopConsumer(consumerId);

            if (_cliOptions.StoreOnly)
            {
                Logger.Debug("StoreOnly specified, stopping");
                Stop("DLQ empty");
            }
            else
            {
                Logger.Info("Republishing messages");
                _deadLetterRepublisher.RepublishMessages(_cliOptions.ReprocessFromQueue, _cliOptions.FlushMessages);

                Logger.Info("Total messages republished: " + _deadLetterRepublisher.TotalRepublished);
                Stop("Reprocess completed");
            }
        }

        public override void Stop(string reason)
        {
            if (_deadLetterQueueConsumer != null)
                _deadLetterQueueConsumer.Stop();

            base.Stop(reason);
        }
    }
}
