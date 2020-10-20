using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Execution.JobProcessing.Notifying;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.CohortPackager.Messaging;
using Smi.Common;
using Smi.Common.Execution;
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


        public CohortPackagerHost(
            GlobalOptions globals,
            ExtractJobStore jobStore = null,
            [CanBeNull] IFileSystem fileSystem = null,
            IJobReporter reporter = null,
            IJobCompleteNotifier notifier = null,
            IRabbitMqAdapter rabbitMqAdapter = null,
            bool loadSmiLogConfig = true
        )
            : base(globals, rabbitMqAdapter, loadSmiLogConfig)
        {
            if (jobStore == null)
            {
                MongoDbOptions mongoDbOptions = Globals.MongoDatabases.ExtractionStoreOptions;
                jobStore = new MongoExtractJobStore(
                    MongoClientHelpers.GetMongoClient(mongoDbOptions, HostProcessName),
                    mongoDbOptions.DatabaseName
                );
            }

            // If not passed a reporter or notifier, try and construct one from the given options

            if (reporter == null)
            {
                if (fileSystem == null)
                    throw new ArgumentException("A filesystem must be provided if the reporter is to be constructed here");

                reporter = GetReporter(
                    Globals.CohortPackagerOptions.ReporterType,
                    jobStore,
                    fileSystem,
                    Globals.FileSystemOptions.ExtractRoot
                );
            }

            notifier ??= GetNotifier(
                Globals.CohortPackagerOptions.NotifierType
            );

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

        private static IJobReporter GetReporter(
            [NotNull] string reporterTypeStr,
            [NotNull] IExtractJobStore jobStore,
            [NotNull] IFileSystem fileSystem,
            [NotNull] string extractRoot
        )
        {
            return reporterTypeStr switch
            {
                nameof(FileReporter) => new FileReporter(
                    jobStore,
                    fileSystem,
                    extractRoot
                ),
                nameof(LoggingReporter) => new LoggingReporter(
                    jobStore
                ),
                _ => throw new ArgumentException($"No case for type, or invalid type string '{reporterTypeStr}'")
            };
        }

        private static IJobCompleteNotifier GetNotifier(
            [NotNull] string notifierTypeStr
        )
        {
            return notifierTypeStr switch
            {
                nameof(LoggingNotifier) => new LoggingNotifier(),
                _ => throw new ArgumentException($"No case for type, or invalid type string '{notifierTypeStr}'")
            };
        }
    }
}
