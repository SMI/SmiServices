using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Messaging;
using Smi.Common.Execution;
using Smi.Common.Options;
using System;
using System.IO.Abstractions;

namespace Microservices.CohortPackager.Execution
{
    public class CohortPackagerHost : MicroserviceHost
    {
        /// <summary>
        /// The process which monitors for extract jobs being completed
        /// </summary>
        public readonly ExtractJobWatcher JobWatcher;

        private readonly ExtractionRequestInfoMessageConsumer _requestInfoMessageConsumer;
        private readonly ExtractFileCollectionMessageConsumer _fileCollectionMessageConsumer;
        private readonly AnonVerificationMessageConsumer _anonVerificationMessageConsumer;
        private readonly AnonFailedMessageConsumer _anonFailedMessageConsumer;


        public CohortPackagerHost(GlobalOptions globals, IFileSystem overrideFileSystem = null, bool loadSmiLogConfig = true)
            : base(globals, loadSmiLogConfig)
        {
            // Connect to store & validate etc.
            var jobStore = new MongoExtractJobStore(globals.MongoDatabases.ExtractionStoreOptions);

            // Setup the watcher for completed jobs
            JobWatcher = new ExtractJobWatcher(globals.CohortPackagerOptions, globals.FileSystemOptions, jobStore, ExceptionCallback, overrideFileSystem ?? new FileSystem());

            AddControlHandler(new CohortPackagerControlMessageHandler(JobWatcher));

            // Setup our consumers
            _requestInfoMessageConsumer = new ExtractionRequestInfoMessageConsumer(jobStore);
            _fileCollectionMessageConsumer = new ExtractFileCollectionMessageConsumer(jobStore);
            _anonFailedMessageConsumer = new AnonFailedMessageConsumer(jobStore);
            _anonVerificationMessageConsumer = new AnonVerificationMessageConsumer(jobStore);
        }

        public override void Start()
        {
            Logger.Debug("Starting host");

            JobWatcher.Start();

            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.ExtractRequestInfoOptions, _requestInfoMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.FileCollectionInfoOptions, _fileCollectionMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.AnonFailedOptions, _anonFailedMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.VerificationStatusOptions, _anonVerificationMessageConsumer, isSolo: true);
        }

        public override void Stop(string reason)
        {
            JobWatcher.StopProcessing("Host - " + reason);

            base.Stop(reason);
        }

        private void ExceptionCallback(Exception e)
        {
            Fatal("ExtractJobWatcher threw an exception", e);
        }
    }
}
