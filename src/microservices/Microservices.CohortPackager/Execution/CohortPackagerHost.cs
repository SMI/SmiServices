using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Execution.JobProcessing.Notifying;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.CohortPackager.Messaging;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Helpers;
using Smi.Common.MongoDB;
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
        private readonly ExtractJobWatcher _jobWatcher;

        private readonly ExtractionRequestInfoMessageConsumer _requestInfoMessageConsumer;
        private readonly ExtractFileCollectionMessageConsumer _fileCollectionMessageConsumer;
        private readonly AnonVerificationMessageConsumer _anonVerificationMessageConsumer;
        private readonly AnonFailedMessageConsumer _anonFailedMessageConsumer;


        /// <summary>
        /// Default constructor for CohortPackagerHost
        /// </summary>
        /// <param name="globals"></param>
        /// <param name="jobStore"></param>
        /// <param name="fileSystem"></param>
        /// <param name="reporter">
        /// Pass to override the default IJobReporter that will be created from
        /// Globals.CohortPackagerOptions.ReportFormat. That value should not be set if a reporter is passed.
        /// </param>
        /// <param name="notifier"></param>
        /// <param name="messageBroker"></param>
        /// <param name="dateTimeProvider"></param>
        public CohortPackagerHost(
            GlobalOptions globals,
            IExtractJobStore? jobStore = null,
            IFileSystem? fileSystem = null,
            IJobReporter? reporter = null,
            IJobCompleteNotifier? notifier = null,
            IMessageBroker? messageBroker = null,
            DateTimeProvider? dateTimeProvider = null
        )
            : base(globals, messageBroker)
        {
            var cohortPackagerOptions = globals.CohortPackagerOptions ??
                throw new ArgumentNullException(nameof(globals), "CohortPackagerOptions cannot be null");

            if (jobStore == null)
            {
                MongoDbOptions mongoDbOptions = Globals.MongoDatabases?.ExtractionStoreOptions
                    ?? throw new ArgumentException("Some part of Globals.MongoDatabases.ExtractionStoreOptions is null");

                jobStore = new MongoExtractJobStore(
                    MongoClientHelpers.GetMongoClient(mongoDbOptions, HostProcessName),
                    mongoDbOptions.DatabaseName!,
                    dateTimeProvider
                );
            }
            else if (dateTimeProvider != null)
                throw new ArgumentException("jobStore and dateTimeProvider are mutually exclusive arguments");

            if (reporter == null)
            {
                // Globals.FileSystemOptions checked in base constructor
                var extractRoot = Globals.FileSystemOptions!.ExtractRoot;
                if (string.IsNullOrWhiteSpace(extractRoot))
                    throw new ArgumentOutOfRangeException(nameof(Globals.FileSystemOptions.ExtractRoot));

                reporter = new JobReporter(
                    jobStore,
                    fileSystem ?? new FileSystem(),
                    extractRoot,
                    cohortPackagerOptions.ReportNewLine
                );
            }

            notifier ??= JobCompleteNotifierFactory.GetNotifier(
                cohortPackagerOptions.NotifierType!
            );

            _jobWatcher = new ExtractJobWatcher(
                cohortPackagerOptions,
                jobStore,
                ExceptionCallback,
                notifier,
                reporter
            );

            AddControlHandler(new CohortPackagerControlMessageHandler(_jobWatcher));

            var maxUnacknowledgedMessages = cohortPackagerOptions.VerificationStatusOptions?.QoSPrefetchCount ??
                throw new ArgumentNullException(nameof(globals), "CohortPackagerOptions.VerificationStatusOptions cannot be null");

            // Setup our consumers

            _requestInfoMessageConsumer = new ExtractionRequestInfoMessageConsumer(jobStore);
            _fileCollectionMessageConsumer = new ExtractFileCollectionMessageConsumer(jobStore);
            _anonFailedMessageConsumer = new AnonFailedMessageConsumer(jobStore);

            var verificationMessageQueueFlushTime =
                (cohortPackagerOptions.VerificationMessageQueueFlushTimeSeconds != null)
                ? TimeSpan.FromSeconds((double)cohortPackagerOptions.VerificationMessageQueueFlushTimeSeconds)
                : CohortPackagerOptions.DefaultVerificationMessageQueueFlushTime;

            _anonVerificationMessageConsumer = new AnonVerificationMessageConsumer(
                jobStore,
                cohortPackagerOptions.VerificationMessageQueueProcessBatches,
                maxUnacknowledgedMessages,
                verificationMessageQueueFlushTime
            );
        }

        public override void Start()
        {
            Logger.Debug("Starting host");

            _jobWatcher.Start();

            // TODO(rkm 2020-03-02) Once this is transactional, we can have one "master" service which actually does the job checking
            MessageBroker.StartConsumer(Globals.CohortPackagerOptions!.ExtractRequestInfoOptions!, _requestInfoMessageConsumer, isSolo: true);
            MessageBroker.StartConsumer(Globals.CohortPackagerOptions.FileCollectionInfoOptions!, _fileCollectionMessageConsumer, isSolo: true);
            MessageBroker.StartConsumer(Globals.CohortPackagerOptions.NoVerifyStatusOptions!, _anonFailedMessageConsumer, isSolo: true);
            MessageBroker.StartConsumer(Globals.CohortPackagerOptions.VerificationStatusOptions!, _anonVerificationMessageConsumer, isSolo: true);
        }

        public override void Stop(string reason)
        {
            _jobWatcher.StopProcessing("Host - " + reason);

            _anonVerificationMessageConsumer.Dispose();

            base.Stop(reason);
        }

        private void ExceptionCallback(Exception e)
        {
            Fatal("ExtractJobWatcher threw an exception", e);
        }
    }
}
