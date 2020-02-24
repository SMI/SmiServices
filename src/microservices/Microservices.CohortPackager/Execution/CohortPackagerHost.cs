using System;
using System.IO.Abstractions;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Messaging;
using MongoDB.Driver;
using Smi.Common.Execution;
using Smi.Common.MongoDB;
using Smi.Common.Options;


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
            MongoDbOptions opts = Globals.MongoDatabases.ExtractionStoreOptions;
            IMongoDatabase database = MongoClientHelpers.GetMongoClient(opts, "CohortPackager").GetDatabase(opts.DatabaseName);
            var jobStore = new MongoExtractJobStore(database);

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

            // NOTE(rkm 2020-02-06) For now, there can be only 1 instance of this service managing a single extraction database
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
