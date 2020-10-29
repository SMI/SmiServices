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
        [NotNull] private readonly string _extractRoot;

        [NotNull] private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Used to ensure any open streams are tidied-up on crashes
        /// </summary>
        [CanBeNull] private Stream _currentFileStream;


        public FileReporter(
            [NotNull] IExtractJobStore jobStore,
            [NotNull] IFileSystem fileSystem,
            [NotNull] string extractRoot,
            ReportFormat reportFormat
        )
            : base(
                jobStore,
                reportFormat
            )
        {
            _fileSystem = fileSystem;
            _extractRoot = extractRoot ?? throw new ArgumentNullException(nameof(extractRoot));
        }


        protected override Stream GetStreamForSummary(ExtractJobInfo jobInfo)
        {
            _currentFileStream = null;
            Stream fileStream;

            if (ShouldWriteCombinedReport(jobInfo))
            {
                // Write a single report

                string jobReportPath = GetSingleReportJobPath(jobInfo);
                if (_fileSystem.File.Exists(jobReportPath))
                    throw new ApplicationException($"Report file '{jobReportPath}' already exists");
                Logger.Info($"Writing report to {jobReportPath}");

                fileStream = _fileSystem.File.OpenWrite(jobReportPath);
                _currentFileStream = fileStream; // This is re-used
            }
            else
            {
                // Create a new directory for the report files, and return an open stream to the summary README

                string jobReportDir = GetMultiReportJobDirectory(jobInfo);
                if (_fileSystem.Directory.Exists(jobReportDir))
                    throw new ApplicationException($"Report directory '{jobReportDir}' already exists");
                _fileSystem.Directory.CreateDirectory(jobReportDir);
                Logger.Info($"Writing reports to directory {jobReportDir}");

                string jobReadmePath = _fileSystem.Path.Combine(jobReportDir, "README.md");
                fileStream = _fileSystem.File.OpenWrite(jobReadmePath);
            }

            return fileStream;
        }

        protected override Stream GetStreamForPixelDataSummary(ExtractJobInfo jobInfo) => GetStream(jobInfo, "pixel_data_summary.csv");
        protected override Stream GetStreamForPixelDataFull(ExtractJobInfo jobInfo) => GetStream(jobInfo, "pixel_data_full.csv");
        protected override Stream GetStreamForTagDataSummary(ExtractJobInfo jobInfo) => GetStream(jobInfo, "tag_data_summary.csv");
        protected override Stream GetStreamForTagDataFull(ExtractJobInfo jobInfo) => GetStream(jobInfo, "tag_data_full.csv");

        private string GetSingleReportJobPath(ExtractJobInfo jobInfo)
            => _fileSystem.Path.Combine(
                _extractRoot,
                jobInfo.ProjectExtractionDir(),
                "reports",
                $"{jobInfo.ExtractionName()}_report.txt"
            );

        private string GetMultiReportJobDirectory(ExtractJobInfo jobInfo)
            => _fileSystem.Path.Combine(
                _extractRoot,
                jobInfo.ProjectExtractionDir(),
                "reports",
                jobInfo.ExtractionName()
            );

        private Stream GetStream(ExtractJobInfo jobInfo, string fileName)
        {
            if (ShouldWriteCombinedReport(jobInfo))
                return _currentFileStream;

            string jobReadmePath = _fileSystem.Path.Combine(GetMultiReportJobDirectory(jobInfo), fileName);
            return _fileSystem.File.OpenWrite(jobReadmePath);
        }

        protected override void FinishReportPart(Stream stream) { }

        protected override void ReleaseUnmanagedResources()
        {
            if (_currentFileStream != null)
                _currentFileStream.Dispose();
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~FileReporter() => ReleaseUnmanagedResources();
    }
}