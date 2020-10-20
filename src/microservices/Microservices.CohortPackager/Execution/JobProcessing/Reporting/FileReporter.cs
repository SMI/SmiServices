using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using System;
using System.IO;
using System.IO.Abstractions;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    [UsedImplicitly]
    public class FileReporter : JobReporterBase
    {
        private readonly string _extractRoot;

        private readonly IFileSystem _fileSystem;

        private Stream _fileStream;

        public FileReporter(
            [NotNull] IExtractJobStore jobStore,
            [NotNull] IFileSystem fileSystem,
            [NotNull] string extractRoot
        )
            : base(jobStore)
        {
            _fileSystem = fileSystem;
            _extractRoot = extractRoot ?? throw new ArgumentNullException(nameof(extractRoot));
        }

        protected override Stream GetStream(ExtractJobInfo jobInfo)
        {
            string jobReportPath = _fileSystem.Path.Combine(_extractRoot, jobInfo.ProjectExtractionDir(), "reports", $"{jobInfo.ExtractionName()}_report.txt");
            if (_fileSystem.File.Exists(jobReportPath))
                throw new ApplicationException($"Report file '{jobReportPath}' already exists");

            _fileStream = _fileSystem.File.OpenWrite(jobReportPath);
            Logger.Info($"Writing report to {jobReportPath}");
            return _fileStream;
        }

        protected override void FinishReport(Stream stream) { }

        protected override void ReleaseUnmanagedResources()
        {
            if (_fileStream != null)
                _fileStream.Dispose();
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~FileReporter() => ReleaseUnmanagedResources();
    }
}