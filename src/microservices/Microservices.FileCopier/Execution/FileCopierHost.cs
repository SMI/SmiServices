using JetBrains.Annotations;
using Microservices.FileCopier.Messaging;
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.IO;
using System.IO.Abstractions;


namespace Microservices.FileCopier.Execution
{
    public class FileCopierHost : MicroserviceHost
    {
        private readonly FileCopyQueueConsumer _consumer;

        public FileCopierHost(
            [NotNull] GlobalOptions options,
            [CanBeNull]IFileSystem fileSystem = null,
            bool loadSmiLogConfig = true
            )
        : base(
            options,
            loadSmiLogConfig: loadSmiLogConfig
            )
        {
            Logger.Debug("Creating FileCopierHost with FileSystemRoot: " + Globals.FileSystemOptions.FileSystemRoot);

            IProducerModel copyStatusProducerModel = RabbitMqAdapter.SetupProducer(Globals.FileCopierOptions.CopyStatusProducerOptions, isBatch: false);

            var fileCopier = new ExtractionFileCopier(
                Globals.FileCopierOptions,
                copyStatusProducerModel,
                Globals.FileSystemOptions.FileSystemRoot,
                Globals.FileSystemOptions.ExtractRoot,
                fileSystem
            );
            _consumer = new FileCopyQueueConsumer(fileCopier);
        }

        public override void Start()
        {
            RabbitMqAdapter.StartConsumer(Globals.FileCopierOptions, _consumer, isSolo: false);
        }
    }
}
