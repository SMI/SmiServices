using SmiServices.Common.Execution;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System.IO.Abstractions;


namespace SmiServices.Microservices.FileCopier
{
    public class FileCopierHost : MicroserviceHost
    {
        private readonly FileCopyQueueConsumer _consumer;

        public FileCopierHost(
            GlobalOptions options,
            IFileSystem? fileSystem = null
        )
        : base(
            options,
            fileSystem ?? new FileSystem()
        )
        {
            Logger.Debug("Creating FileCopierHost with FileSystemRoot: " + Globals.FileSystemOptions!.FileSystemRoot);

            IProducerModel copyStatusProducerModel = MessageBroker.SetupProducer(Globals.FileCopierOptions!.CopyStatusProducerOptions!, isBatch: false);

            var fileCopier = new ExtractionFileCopier(
                Globals.FileCopierOptions,
                copyStatusProducerModel,
                Globals.FileSystemOptions.FileSystemRoot!,
                Globals.FileSystemOptions.ExtractRoot!,
                FileSystem
            );
            _consumer = new FileCopyQueueConsumer(fileCopier);
        }

        public override void Start()
        {
            MessageBroker.StartConsumer(Globals.FileCopierOptions!, _consumer, isSolo: false);
        }
    }
}
