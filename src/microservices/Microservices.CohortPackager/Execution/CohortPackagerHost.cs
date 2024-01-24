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
using System.Text.RegularExpressions;


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
        /// <param name="rabbitMqAdapter"></param>
        /// <param name="dateTimeProvider"></param>
        public CohortPackagerHost(
            GlobalOptions globals,
            ExtractJobStore? jobStore = null,
            IFileSystem? fileSystem = null,
            IJobReporter? reporter = null,
            IJobCompleteNotifier? notifier = null,
            IRabbitMqAdapter? rabbitMqAdapter = null,
            DateTimeProvider? dateTimeProvider = null
        )
            : base(globals, rabbitMqAdapter)
        {
            var cohortPackagerOptions = globals.CohortPackagerOptions ??
                throw new ArgumentNullException(nameof(globals), "CohortPackagerOptions cannot be null");

            if (jobStore == null)
            {
                MongoDbOptions mongoDbOptions = Globals.MongoDatabases?.ExtractionStoreOptions
                    ?? throw new ArgumentException("Some part of Globals.MongoDatabases.ExtractionStoreOptions is null");

                var verificationMessageQueueFlushTime =
                    (cohortPackagerOptions.VerificationMessageQueueFlushTimeSeconds != null)
                    ? TimeSpan.FromSeconds((double)cohortPackagerOptions.VerificationMessageQueueFlushTimeSeconds)
                    : CohortPackagerOptions.DefaultVerificationMessageQueueFlushTime;

                jobStore = new MongoExtractJobStore(
                    MongoClientHelpers.GetMongoClient(mongoDbOptions, HostProcessName),
                    mongoDbOptions.DatabaseName!,
                    cohortPackagerOptions.VerificationMessageQueueProcessBatches,
                    verificationMessageQueueFlushTime,
                    dateTimeProvider
                );
            }
            else if (dateTimeProvider != null)
                throw new ArgumentException("jobStore and dateTimeProvider are mutually exclusive arguments");

            // If not passed a reporter or notifier, try and construct one from the given options

            string reportFormatStr = cohortPackagerOptions.ReportFormat
                ?? throw new ArgumentException("Some part of Globals.CohortPackagerOptions.ReportFormat is null");
            if (reporter == null)
            {
                reporter = JobReporterFactory.GetReporter(
                    cohortPackagerOptions.ReporterType!,
                    jobStore,
                    fileSystem ?? new FileSystem(),
                    Globals.FileSystemOptions!.ExtractRoot!,
                    reportFormatStr,
                    Regex.Unescape(cohortPackagerOptions.ReportNewLine!)
                );
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(reportFormatStr))
                    throw new ArgumentException($"Passed an IJobReporter, but this conflicts with the ReportFormat of '{reportFormatStr}' in the given options");
                if (fileSystem != null)
                    throw new ArgumentException("Passed a fileSystem, but this will be unused as also passed an existing IJobReporter");
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
            _anonVerificationMessageConsumer = new AnonVerificationMessageConsumer(jobStore, maxUnacknowledgedMessages);
        }

        public override void Start()
        {
            Logger.Debug("Starting host");

            _jobWatcher.Start();

            // TODO(rkm 2020-03-02) Once this is transactional, we can have one "master" service which actually does the job checking
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions!.ExtractRequestInfoOptions!, _requestInfoMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.FileCollectionInfoOptions!, _fileCollectionMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.NoVerifyStatusOptions!, _anonFailedMessageConsumer, isSolo: true);
            RabbitMqAdapter.StartConsumer(Globals.CohortPackagerOptions.VerificationStatusOptions!, _anonVerificationMessageConsumer, isSolo: true);
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
