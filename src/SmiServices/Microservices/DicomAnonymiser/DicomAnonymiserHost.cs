using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser.Anonymisers;
using System;
using System.IO.Abstractions;
using SmiServices.Common.Messages.Extraction;

namespace SmiServices.Microservices.DicomAnonymiser
{
    public class DicomAnonymiserHost : MicroserviceHost
    {
        private readonly IDicomAnonymiser _anonymiser;
        private readonly DicomAnonymiserConsumer _consumer;

        public DicomAnonymiserHost(
            GlobalOptions options,
            IDicomAnonymiser? anonymiser = null,
            IFileSystem? fileSystem = null
        )
            : base(options)
        {
            _anonymiser = anonymiser ?? AnonymiserFactory.CreateAnonymiser(options!);

            var producerModel = MessageBroker.SetupProducer<ExtractedFileStatusMessage>(options.DicomAnonymiserOptions!.ExtractFileStatusProducerOptions!, isBatch: false);

            _consumer = new DicomAnonymiserConsumer(
                Globals.DicomAnonymiserOptions!,
                Globals.FileSystemOptions!.FileSystemRoot!,
                Globals.FileSystemOptions.ExtractRoot!,
                _anonymiser,
                producerModel,
                fileSystem
            );
        }

        public override void Start()
        {
            MessageBroker.StartConsumer(Globals.DicomAnonymiserOptions!.AnonFileConsumerOptions!, _consumer, isSolo: false);
        }

        public override void Stop(string reason)
        {
            if (_anonymiser is IDisposable disposable)
            {
                Logger.Info("Disposing anonymiser");
                disposable.Dispose();
            }

            base.Stop(reason);
        }
    }
}
