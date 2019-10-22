
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Microservices.DicomReprocessor.Execution.Processors;
using Microservices.DicomReprocessor.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microservices.DicomReprocessor.Execution
{
    public class DicomReprocessorHost : MicroserviceHost
    {
        private readonly MongoDbReader _mongoReader;
        private readonly IDocumentProcessor _processor;
        private Task<TimeSpan> _processorTask;

        private readonly string _queryString;
        private readonly bool _autoRun;
        private readonly int _sleepDuration;


        public DicomReprocessorHost(GlobalOptions options, DicomReprocessorCliOptions dicomReprocessorCliOptions, bool loadSmiLogConfig = true)
            : base(options, loadSmiLogConfig)
        {
            string key = dicomReprocessorCliOptions.ReprocessingRoutingKey;

            _sleepDuration = dicomReprocessorCliOptions.SleepTime;

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("ReprocessingRoutingKey");

            IProducerModel reprocessingProducerModel = RabbitMqAdapter.SetupProducer(options.DicomReprocessorOptions.ReprocessingProducerOptions, true);

            Logger.Info("Documents will be reprocessed to " +
                        options.DicomReprocessorOptions.ReprocessingProducerOptions.ExchangeName + " on vhost " +
                        options.RabbitOptions.RabbitMqVirtualHost + " with routing key \"" + key + "\"");

            if (!string.IsNullOrWhiteSpace(dicomReprocessorCliOptions.QueryFile))
                _queryString = File.ReadAllText(dicomReprocessorCliOptions.QueryFile);

            //TODO Make this into a CreateInstance<> call
            switch (options.DicomReprocessorOptions.ProcessingMode)
            {
                case ProcessingMode.TagPromotion:
                    _processor = new TagPromotionProcessor(options.DicomReprocessorOptions, reprocessingProducerModel, key);
                    break;

                case ProcessingMode.ImageReprocessing:
                    _processor = new DicomFileProcessor(options.DicomReprocessorOptions, reprocessingProducerModel, key);
                    break;

                case ProcessingMode.Unknown:
                default:
                    throw new ArgumentException("ProcessingMode " + options.DicomReprocessorOptions.ProcessingMode + " not supported");
            }

            _autoRun = dicomReprocessorCliOptions.AutoRun;

            _mongoReader = new MongoDbReader(
                options.MongoDatabases.DicomStoreOptions,
                dicomReprocessorCliOptions,
                HostProcessName + "-" + HostProcessID);
        }

        public override void Start()
        {
            _processorTask = _mongoReader.RunQuery(_queryString, _processor, _sleepDuration, _autoRun);
            TimeSpan queryTime = _processorTask.Result;

            if (_processor.TotalProcessed == 0)
                Logger.Warn("Nothing reprocessed");

            Logger.Info("Total messages sent: " + _processor.TotalProcessed);
            Logger.Info("Total failed to reprocess : " + _processor.TotalFailed);

            if (queryTime != default(TimeSpan))
                Logger.Info("Average documents processed per second: " + Convert.ToInt32(_processor.TotalProcessed / queryTime.TotalSeconds));

            // Only call stop if we exited normally
            if (_mongoReader.WasCancelled)
                return;

            Stop("Reprocessing completed");
        }

        public override void Stop(string reason)
        {
            _mongoReader.Stop();
            _processorTask.Wait();

            base.Stop(reason);
        }
    }
}
