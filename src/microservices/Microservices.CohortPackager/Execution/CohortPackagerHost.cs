using System;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
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
        private readonly ExtractJobWatcher _jobWatcher;

        private readonly ExtractionRequestInfoMessageConsumer _requestInfoMessageConsumer;
        private readonly ExtractFileCollectionMessageConsumer _fileCollectionMessageConsumer;
        private readonly AnonVerificationMessageConsumer _anonVerificationMessageConsumer;
        private readonly AnonFailedMessageConsumer _anonFailedMessageConsumer;


        public CohortPackagerHost(GlobalOptions globals, bool loadSmiLogConfig = true)
            : base(globals, loadSmiLogConfig)
        {
            // Connect to store & validate etc.
            MongoDbOptions opts = Globals.MongoDatabases.ExtractionStoreOptions;
            MongoClient client = MongoClientHelpers.GetMongoClient(opts, HostProcessName);
            var jobStore = new MongoExtractJobStore(client, opts.DatabaseName);

            // Setup the watcher for completed jobs
            _jobWatcher = new ExtractJobWatcher(globals.CohortPackagerOptions, jobStore, ExceptionCallback, new DoNothingJobCompleteNotifier());

            AddControlHandler(new CohortPackagerControlMessageHandler(_jobWatcher));

            // Setup our consumers
            _requestInfoMessageConsumer = new ExtractionRequestInfoMessageConsumer(jobStore);
            _fileCollectionMessageConsumer = new ExtractFileCollectionMessageConsumer(jobStore);
            _anonFailedMessageConsumer = new AnonFailedMessageConsumer(jobStore);
            _anonVerificationMessageConsumer = new AnonVerificationMessageConsumer(jobStore);
        }

        public override void Start()
        {
            Logger.Debug("Starting host");

            _jobWatcher.Start();

            // TODO(rkm 2020-03-02) Once this is transactional, we can have one "master" service which actually does the job checking
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.ExtractRequestInfoOptions, _requestInfoMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.FileCollectionInfoOptions, _fileCollectionMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.AnonFailedOptions, _anonFailedMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.VerificationStatusOptions, _anonVerificationMessageConsumer, isSolo: true);
        }

        public override void Stop(string reason)
        {
            _jobWatcher.StopProcessing("Host - " + reason);

            base.Stop(reason);
        }

        private void ExceptionCallback(Exception e)
        {
            Fatal("ExtractJobWatcher threw an exception", e);
        }
    }
}
