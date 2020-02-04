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

        private readonly ConsumerOptions _extractRequestInfoOptions;
        private readonly ConsumerOptions _extractFilesInfoOptions;
        private readonly ConsumerOptions _anonImageStatusOptions;

        private readonly ExtractionRequestInfoMessageConsumer _requestInfoMessageConsumer;
        private readonly ExtractFileCollectionMessageConsumer _fileCollectionMessageConsumer;
        private readonly AnonVerificationMessageConsumer _anonVerificationMessageConsumer;
        private readonly AnonFailedMessageConsumer _anonFailedMessageConsumer;


        public CohortPackagerHost(GlobalOptions globals, IFileSystem overrideFileSystem = null, bool loadSmiLogConfig = true)
            : base(globals, loadSmiLogConfig)
        {
            _extractRequestInfoOptions = globals.CohortPackagerOptions.ExtractRequestInfoOptions;
            _extractFilesInfoOptions = globals.CohortPackagerOptions.ExtractFilesInfoOptions;
            _anonImageStatusOptions = globals.CohortPackagerOptions.AnonImageStatusOptions;

            // Connect to store & validate etc.
            var jobStore = new MongoExtractJobStore(globals.MongoDatabases.ExtractionStoreOptions);

            // Setup the watcher for completed jobs
            JobWatcher = new ExtractJobWatcher(globals.CohortPackagerOptions, globals.FileSystemOptions, jobStore, ExceptionCallback, overrideFileSystem ?? new FileSystem());

            AddControlHandler(new CohortPackagerControlMessageHandler(JobWatcher));

            // Setup our consumers
            _requestInfoMessageConsumer = new ExtractionRequestInfoMessageConsumer(jobStore);
            _fileCollectionMessageConsumer = new ExtractFileCollectionMessageConsumer(jobStore);
            _anonVerificationMessageConsumer = new AnonVerificationMessageConsumer(jobStore);
            _anonFailedMessageConsumer = new AnonFailedMessageConsumer(jobStore);
        }

        public override void Start()
        {
            Logger.Debug("Starting host");

            JobWatcher.Start();

            RabbitMqAdapter.StartConsumer(_extractRequestInfoOptions, _requestInfoMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(_extractFilesInfoOptions, _fileCollectionMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(_anonImageStatusOptions, _anonVerificationMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(_anonImageStatusOptions, _anonFailedMessageConsumer, isSolo: true);
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
