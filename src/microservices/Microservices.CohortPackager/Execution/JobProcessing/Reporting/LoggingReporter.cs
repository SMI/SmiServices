using System;
using System.IO;
using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NLog;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    /// <summary>
    /// Basic reporter which outputs to its logger. Should be used for testing only
    /// </summary>
    [UsedImplicitly]
    public class LoggingReporter : JobReporterBase
    {
        private readonly ILogger _logger;

        public LoggingReporter(
            [NotNull] IExtractJobStore jobStore,
            string _
        )
            : base(jobStore, _)
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        protected override Stream GetStream(ExtractJobInfo _) => new MemoryStream();

        protected override void FinishReport(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
                _logger.Info(streamReader.ReadToEnd);
        }

        protected override void ReleaseUnmanagedResources() { }
        public override void Dispose() { }
    }
}
