using System;
using System.IO;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Execution.JobProcessing.Notifying;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.CohortPackager.Messaging;
using MongoDB.Driver;
using Smi.Common;
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


        public CohortPackagerHost(
            GlobalOptions globals,
            IJobReporter reporter = null,
            IJobCompleteNotifier notifier = null,
            IRabbitMqAdapter rabbitMqAdapter = null,
            bool loadSmiLogConfig = true
        )
            : base(globals, rabbitMqAdapter, loadSmiLogConfig)
        {

            MongoDbOptions mongoDbOptions = Globals.MongoDatabases.ExtractionStoreOptions;
            MongoClient client = MongoClientHelpers.GetMongoClient(mongoDbOptions, HostProcessName);
            var jobStore = new MongoExtractJobStore(client, mongoDbOptions.DatabaseName);

            string reportDir = $"{Globals.FileSystemOptions.ExtractRoot}/Reports";
            Directory.CreateDirectory(reportDir);

            // If not passed a reporter or notifier, try and construct one from the given options
            if (reporter == null)
                reporter = ObjectFactory.CreateInstance<IJobReporter>($"{typeof(IJobReporter).Namespace}.{Globals.CohortPackagerOptions.ReporterType}", typeof(IJobReporter).Assembly, jobStore, reportDir);
            if (notifier == null)
                notifier = ObjectFactory.CreateInstance<IJobCompleteNotifier>($"{typeof(IJobCompleteNotifier).Namespace}.{Globals.CohortPackagerOptions.NotifierType}", typeof(IJobCompleteNotifier).Assembly, jobStore);

            _jobWatcher = new ExtractJobWatcher(
                globals.CohortPackagerOptions,
                jobStore,
                ExceptionCallback,
                notifier,
                reporter);

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
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.NoVerifyStatusOptions, _anonFailedMessageConsumer, isSolo: true);
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
