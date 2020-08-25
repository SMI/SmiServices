using JetBrains.Annotations;
using Microservices.FileCopier.Messaging;
using Smi.Common.Execution;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.IO;

namespace Microservices.FileCopier.Execution
{
    public class FileCopierHost : MicroserviceHost
    {
        private readonly FileCopyQueueConsumer _consumer;

        public FileCopierHost(
            [NotNull] GlobalOptions options,
            bool loadSmiLogConfig = true)
            : base(options, loadSmiLogConfig: loadSmiLogConfig)
        {
            if (!Directory.Exists(Globals.FileSystemOptions.FileSystemRoot))
                throw new ArgumentException($"Cannot find the specified FileSystemRoot: '{Globals.FileSystemOptions.FileSystemRoot}'");

            Logger.Debug("Creating FileCopierHost with FileSystemRoot: " + Globals.FileSystemOptions.FileSystemRoot);

            IProducerModel copyStatusProducerModel = RabbitMqAdapter.SetupProducer(Globals.FileCopierOptions.CopyStatusProducerOptions, isBatch: false);

            var fileCopier = new FileCopier(copyStatusProducerModel, Globals.FileSystemOptions.FileSystemRoot);
            _consumer = new FileCopyQueueConsumer(fileCopier);
        }

        public override void Start()
        {
            RabbitMqAdapter.StartConsumer(Globals.FileCopierOptions, _consumer, isSolo: false);
        }
    }
}
