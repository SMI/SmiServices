using Microservices.DicomAnonymiser.Anonymisers;
using Smi.Common.Execution;
using Smi.Common.Options;
using System;
using System.IO.Abstractions;

namespace Microservices.DicomAnonymiser
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
            _anonymiser = anonymiser ?? AnonymiserFactory.CreateAnonymiser(Globals.DicomAnonymiserOptions!);

            var producerModel = MessageBroker.SetupProducer(options.DicomAnonymiserOptions!.ExtractFileStatusProducerOptions!, isBatch: false);

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
