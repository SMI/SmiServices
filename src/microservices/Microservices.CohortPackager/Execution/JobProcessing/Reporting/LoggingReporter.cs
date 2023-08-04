using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NLog;
using System.IO;


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
            IExtractJobStore jobStore,
            ReportFormat reportFormat,
            string? reportNewLine
        )
            : base(
                jobStore,
                reportFormat,
                reportNewLine
            )
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        protected override Stream GetStreamForSummary(ExtractJobInfo jobInfo) => new MemoryStream();
        protected override Stream GetStreamForPixelDataSummary(ExtractJobInfo jobInfo) => new MemoryStream();
        protected override Stream GetStreamForPixelDataFull(ExtractJobInfo jobInfo) => new MemoryStream();
        protected override Stream GetStreamForPixelDataWordLengthFrequencies(ExtractJobInfo jobInfo) => new MemoryStream();
        protected override Stream GetStreamForTagDataSummary(ExtractJobInfo jobInfo) => new MemoryStream();
        protected override Stream GetStreamForTagDataFull(ExtractJobInfo jobInfo) => new MemoryStream();

        protected override void FinishReportPart(Stream stream)
        {
            stream.Position = 0;
            using var streamReader = new StreamReader(stream);
            _logger.Info(streamReader.ReadToEnd);
        }

        protected override void ReleaseUnmanagedResources() { }
        public override void Dispose() { }
    }
}
