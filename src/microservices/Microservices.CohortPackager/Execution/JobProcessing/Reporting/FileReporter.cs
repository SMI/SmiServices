using Microservices.CohortPackager.Execution.ExtractJobStorage;
using System;
using System.IO;
using System.IO.Abstractions;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public class FileReporter : JobReporterBase
    {
        private readonly string _extractRoot;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Used to ensure any open streams are tidied-up on crashes
        /// </summary>
        private Stream? _currentFileStream;


        public FileReporter(
            IExtractJobStore jobStore,
            IFileSystem fileSystem,
            string extractRoot,
            ReportFormat reportFormat,
            string? reportNewLine
        )
            : base(
                jobStore,
                reportFormat,
                reportNewLine
            )
        {
            _fileSystem = fileSystem;
            _extractRoot = extractRoot ?? throw new ArgumentNullException(nameof(extractRoot));
        }

        protected override Stream GetStreamForSummary(ExtractJobInfo jobInfo)
        {
            _currentFileStream = null;
            Stream fileStream;

            string projReportsDirAbsolute = AbsolutePathToProjReportsDir(jobInfo);
            Directory.CreateDirectory(projReportsDirAbsolute);

            if (ShouldWriteCombinedReport(jobInfo))
            {
                // Write a single report
                string jobReportPath = _fileSystem.Path.Combine(projReportsDirAbsolute, $"{jobInfo.ExtractionName()}_report.txt");
                if (_fileSystem.File.Exists(jobReportPath))
                    throw new ApplicationException($"Report file '{jobReportPath}' already exists");
                Logger.Info($"Writing report to {jobReportPath}");

                fileStream = _fileSystem.File.OpenWrite(jobReportPath);
                _currentFileStream = fileStream; // This is re-used
            }
            else
            {
                // Create a new directory for the report files, and return an open stream to the summary README
                string jobReportDir = AbsolutePathToProjExtractionReportsDir(jobInfo);
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
        protected override Stream GetStreamForPixelDataWordLengthFrequencies(ExtractJobInfo jobInfo) => GetStream(jobInfo, "pixel_data_word_frequencies.csv");
        protected override Stream GetStreamForTagDataSummary(ExtractJobInfo jobInfo) => GetStream(jobInfo, "tag_data_summary.csv");
        protected override Stream GetStreamForTagDataFull(ExtractJobInfo jobInfo) => GetStream(jobInfo, "tag_data_full.csv");

        private string AbsolutePathToProjReportsDir(ExtractJobInfo jobInfo)
            =>
                _fileSystem.Path.Combine(
                    _extractRoot,
                    jobInfo.ProjectExtractionDir(),
                    "reports"
                );

        private string AbsolutePathToProjExtractionReportsDir(ExtractJobInfo jobInfo)
            =>
                _fileSystem.Path.Combine(
                    AbsolutePathToProjReportsDir(jobInfo),
                    jobInfo.ExtractionName()
                );

        private Stream GetStream(ExtractJobInfo jobInfo, string fileName)
        {
            if (ShouldWriteCombinedReport(jobInfo))
                return _currentFileStream!;

            string absReportPath = _fileSystem.Path.Combine(AbsolutePathToProjExtractionReportsDir(jobInfo), fileName);
            return _fileSystem.File.OpenWrite(absReportPath);
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
