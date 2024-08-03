
using System;
using System.IO;
using System.IO.Abstractions;
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Microservices.DicomTagReader.Messaging;

namespace Microservices.DicomTagReader.Execution
{
    public class DicomTagReaderHost : MicroserviceHost
    {
        public readonly DicomTagReaderConsumer AccessionDirectoryMessageConsumer;
        private readonly TagReaderBase _tagReader;


        public DicomTagReaderHost(GlobalOptions options)
            : base(options)
        {
            if (!Directory.Exists(options.FileSystemOptions!.FileSystemRoot))
                throw new ArgumentException(
                    $"Cannot find the FileSystemRoot specified in the given MicroservicesOptions ({options.FileSystemOptions.FileSystemRoot})");

            Logger.Debug($"Creating DicomTagReaderHost with FileSystemRoot: {options.FileSystemOptions.FileSystemRoot}");
            Logger.Debug($"NackIfAnyFileErrors option set to {options.DicomTagReaderOptions!.NackIfAnyFileErrors}");

            IProducerModel seriesProducerModel;
            IProducerModel imageProducerModel;

            try
            {
                Logger.Debug(
                    $"Creating seriesProducerModel with ExchangeName: {options.DicomTagReaderOptions.SeriesProducerOptions!.ExchangeName}");
                seriesProducerModel = MessageBroker.SetupProducer(options.DicomTagReaderOptions.SeriesProducerOptions, true);

                Logger.Debug(
                    $"Creating imageProducerModel with ExchangeName: {options.DicomTagReaderOptions.ImageProducerOptions!.ExchangeName}");
                imageProducerModel = MessageBroker.SetupProducer(options.DicomTagReaderOptions.ImageProducerOptions, true);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Couldn't create series producer model on startup", e);
            }

            Logger.Debug("Creating AccessionDirectoryMessageConsumer");

            _tagReader = options.DicomTagReaderOptions.TagProcessorMode switch
            {
                TagProcessorMode.Serial => new SerialTagReader(options.DicomTagReaderOptions, options.FileSystemOptions, seriesProducerModel, imageProducerModel, new FileSystem()),
                TagProcessorMode.Parallel => new ParallelTagReader(options.DicomTagReaderOptions, options.FileSystemOptions, seriesProducerModel, imageProducerModel, new FileSystem()),
                _ => throw new ArgumentException($"No case for mode {options.DicomTagReaderOptions.TagProcessorMode}"),
            };

            // Setup our consumer
            AccessionDirectoryMessageConsumer = new DicomTagReaderConsumer(_tagReader, options);
        }

        public override void Start()
        {
            // Start the consumer to await callbacks when messages arrive
            MessageBroker.StartConsumer(Globals.DicomTagReaderOptions!, AccessionDirectoryMessageConsumer, isSolo: false);
            Logger.Debug("Consumer started");
        }

        public override void Stop(string reason)
        {
            _tagReader.Stop();
            base.Stop(reason);
        }
    }
}
