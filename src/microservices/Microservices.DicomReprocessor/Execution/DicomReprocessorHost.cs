using System;
using System.IO;
using System.Threading.Tasks;
using Microservices.DicomReprocessor.Execution.Processors;
using Microservices.DicomReprocessor.Options;
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;

namespace Microservices.DicomReprocessor.Execution
{
    public class DicomReprocessorHost : MicroserviceHost
    {
        private readonly MongoDbReader _mongoReader;
        private readonly IDocumentProcessor _processor;
        private Task<TimeSpan>? _processorTask;

        private readonly string? _queryString;

        public DicomReprocessorHost(GlobalOptions options, DicomReprocessorCliOptions cliOptions)
            : base(options)
        {
            string? key = cliOptions.ReprocessingRoutingKey;

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("ReprocessingRoutingKey");

            // Set the initial sleep time
            Globals.DicomReprocessorOptions!.SleepTime = TimeSpan.FromMilliseconds(cliOptions.SleepTimeMs);

            IProducerModel reprocessingProducerModel = RabbitMqAdapter.SetupProducer(options.DicomReprocessorOptions!.ReprocessingProducerOptions!, true);

            Logger.Info("Documents will be reprocessed to " +
                        options.DicomReprocessorOptions.ReprocessingProducerOptions!.ExchangeName + " on vhost " +
                        options.RabbitOptions!.RabbitMqVirtualHost + " with routing key \"" + key + "\"");

            if (!string.IsNullOrWhiteSpace(cliOptions.QueryFile))
                _queryString = File.ReadAllText(cliOptions.QueryFile);

            //TODO Make this into a CreateInstance<> call
            switch (options.DicomReprocessorOptions.ProcessingMode)
            {
                case ProcessingMode.TagPromotion:
                    _processor = new TagPromotionProcessor(options.DicomReprocessorOptions, reprocessingProducerModel, key);
                    break;

                case ProcessingMode.ImageReprocessing:
                    _processor = new DicomFileProcessor(options.DicomReprocessorOptions, reprocessingProducerModel, key);
                    break;

                default:
                    throw new ArgumentException("ProcessingMode " + options.DicomReprocessorOptions.ProcessingMode + " not supported");
            }

            _mongoReader = new MongoDbReader(options.MongoDatabases!.DicomStoreOptions!, cliOptions, HostProcessName + "-" + HostProcessID);

            AddControlHandler(new DicomReprocessorControlMessageHandler(Globals.DicomReprocessorOptions));
        }

        public override void Start()
        {
            _processorTask = _mongoReader.RunQuery(_queryString, _processor, Globals.DicomReprocessorOptions!);
            TimeSpan queryTime = _processorTask.Result;

            if (_processor.TotalProcessed == 0)
                Logger.Warn("Nothing reprocessed");
            else
                _processor.LogProgress();
            
            if (queryTime != default)
                Logger.Info("Average documents processed per second: " + Convert.ToInt32(_processor.TotalProcessed / queryTime.TotalSeconds));

            // Only call stop if we exited normally
            if (_mongoReader.WasCancelled)
                return;

            Stop("Reprocessing completed");
        }

        public override void Stop(string reason)
        {
            _mongoReader.Stop();

            try
            {
                _processorTask!.Wait();
            }
            catch (AggregateException e)
            {
                Logger.Error(e, "Exceptions thrown by ProcessorTask during Stop (Stop Reason Was {0})", reason);
            }

            base.Stop(reason);
        }
    }
}
