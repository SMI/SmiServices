using SmiServices.Common.Execution;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SmiServices.Microservices.DicomReprocessor
{
    public class DicomReprocessorHost : MicroserviceHost
    {
        private readonly MongoDbReader _mongoReader;
        private readonly IDocumentProcessor _processor;
        private Task<TimeSpan>? _processorTask;

        private readonly string? _queryString;

        public DicomReprocessorHost(GlobalOptions options, DicomReprocessorCliOptions cliOptions, IFileSystem? fileSystem = null)
            : base(options, fileSystem ?? new FileSystem())
        {
            string? key = cliOptions.ReprocessingRoutingKey;

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("ReprocessingRoutingKey");

            // Set the initial sleep time
            Globals.DicomReprocessorOptions!.SleepTime = TimeSpan.FromMilliseconds(cliOptions.SleepTimeMs);

            IProducerModel reprocessingProducerModel = MessageBroker.SetupProducer(options.DicomReprocessorOptions!.ReprocessingProducerOptions!, true);

            Logger.Info("Documents will be reprocessed to " +
                        options.DicomReprocessorOptions.ReprocessingProducerOptions!.ExchangeName + " on vhost " +
                        options.RabbitOptions!.RabbitMqVirtualHost + " with routing key \"" + key + "\"");

            if (!string.IsNullOrWhiteSpace(cliOptions.QueryFile))
                _queryString = FileSystem.File.ReadAllText(cliOptions.QueryFile);

            //TODO Make this into a CreateInstance<> call
            _processor = options.DicomReprocessorOptions.ProcessingMode switch
            {
                ProcessingMode.TagPromotion => new TagPromotionProcessor(reprocessingProducerModel),
                ProcessingMode.ImageReprocessing => new DicomFileProcessor(reprocessingProducerModel, key),
                _ => throw new ArgumentException("ProcessingMode " + options.DicomReprocessorOptions.ProcessingMode + " not supported"),
            };
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
