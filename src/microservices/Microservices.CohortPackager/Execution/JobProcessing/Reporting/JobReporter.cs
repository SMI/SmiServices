using CsvHelper;
using CsvHelper.Configuration;
using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Reporting.Destinations;
using Microservices.IsIdentifiable.Reporting.Reports;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public class JobReporter
    {
        [NotNull] private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        [NotNull] private readonly IExtractJobStore _jobStore;

        [NotNull] private readonly IFileSystem _fileSystem;
        [NotNull] private readonly string _extractRoot;

        [NotNull] private readonly string _reportNewLine;

        [NotNull] private readonly CsvConfiguration _csvConfiguration;

        private const string ERRORS_FILE_NAME = "processing_errors.md";

        public JobReporter(
            [NotNull] IExtractJobStore jobStore,
            [NotNull] IFileSystem fileSystem,
            [NotNull] string extractRoot,
            [CanBeNull] string reportNewLine
        )
        {
            _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
            _fileSystem = fileSystem;
            _extractRoot = extractRoot ?? throw new ArgumentNullException(nameof(extractRoot));

            // NOTE(rkm 2020-11-20) IsNullOrWhiteSpace returns true for newline characters!
            if (!string.IsNullOrEmpty(reportNewLine))
            {
                if (reportNewLine.Contains(@"\"))
                    throw new ArgumentException("ReportNewLine contained an escaped backslash");

                _reportNewLine = reportNewLine;
            }
            else
            {
                // NOTE(rkm 2021-04-06) Escape the newline here so it prints correctly...
                _logger.Warn($"Not passed a specific newline string for creating reports. Defaulting to Environment.NewLine ('{Regex.Escape(Environment.NewLine)}')");
                // ... and just use the (unescaped) value as-is
                _reportNewLine = Environment.NewLine;
            }

            // TODO(rkm 2022-03-17) Pass this to CsvDestination after https://github.com/SMI/IsIdentifiable/issues/44
            _csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                NewLine = _reportNewLine,
            };
        }

        public void CreateReports(Guid jobId)
        {
            CompletedExtractJobInfo completedJobInfo = _jobStore.GetCompletedJobInfo(jobId);
            _logger.Info($"Creating reports for {jobId}");

            string jobReportsDirAbsolute = _fileSystem.Path.Combine(
                _extractRoot,
                completedJobInfo.ProjectExtractionDir(),
                "reports",
                completedJobInfo.ExtractionName()
            );

            if (_fileSystem.Directory.Exists(jobReportsDirAbsolute))
                throw new ApplicationException($"Job reports directory '{jobReportsDirAbsolute}' already exists");
            _fileSystem.Directory.CreateDirectory(jobReportsDirAbsolute);

            WriteReadme(completedJobInfo, jobReportsDirAbsolute);

            if (completedJobInfo.IsIdentifiableExtraction)
            {
                WriteMissingFileList(completedJobInfo, jobReportsDirAbsolute);
                return;
            }

            WriteRejectionsCsv(completedJobInfo, jobReportsDirAbsolute);

            var hadErrors = WriteProcessingErrorsMarkdown(completedJobInfo, jobReportsDirAbsolute);
            if (hadErrors)
            {
                _logger.Warn($"Job {jobId} had errors during proecssing. Check {ERRORS_FILE_NAME}");
            }

            WriteVerificationFailuresCsv(completedJobInfo, jobReportsDirAbsolute);

            _logger.Info($"Reports for {jobId} created");
        }

        private void WriteReadme(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string identExtraction = jobInfo.IsIdentifiableExtraction ? "Yes" : "No";
            string filteredExtraction = !jobInfo.IsNoFilterExtraction ? "Yes" : "No";
            var readmeLines = new List<string>
            {
                $"# SMI extraction reports for {jobInfo.ProjectNumber}/{jobInfo.ExtractionName()}",
                "",
                "Job info:",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Job submitted at:             {jobInfo.JobSubmittedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {jobInfo.JobCompletedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {(jobInfo.JobCompletedAt - jobInfo.JobSubmittedAt)}",
                $"-   Extraction tag:               {jobInfo.KeyTag}",
                $"-   Extraction modality:          {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"-   Requested identifier count:   {jobInfo.KeyValueCount}",
                $"-   Identifiable extraction:      {identExtraction}",
                $"-   Filtered extraction:          {filteredExtraction}",
            };

            string jobReadmePath = _fileSystem.Path.Combine(jobReportsDirAbsolute, "README.md");
            using var fileStream = _fileSystem.File.OpenWrite(jobReadmePath);
            using var streamWriter = GetStreamWriter(fileStream);

            foreach (string line in readmeLines)
                streamWriter.WriteLine(line);
        }

        private void WriteRejectionsCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string blockedFilesPath = _fileSystem.Path.Combine(jobReportsDirAbsolute, "blocked_files.md");
            using var fileStream = _fileSystem.File.OpenWrite(blockedFilesPath);

            IEnumerable<ExtractionIdentifierRejectionInfo> rejects = _jobStore.GetCompletedJobRejections(jobInfo.ExtractionJobIdentifier);
            WriteCsv(fileStream, JobRejectionCsvRecord.FromExtractionIdentifierRejectionInfos(rejects));
        }

        private bool WriteProcessingErrorsMarkdown(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string proecssingErrorsPath = _fileSystem.Path.Combine(jobReportsDirAbsolute, ERRORS_FILE_NAME);
            using var fileStream = _fileSystem.File.OpenWrite(proecssingErrorsPath);
            using var streamWriter = GetStreamWriter(fileStream);

            var errors = _jobStore.GetCompletedJobAnonymisationFailures(jobInfo.ExtractionJobIdentifier).ToList();

            if (!errors.Any())
                return false;

            streamWriter.WriteLine($"# SMI extraction processing errors for {jobInfo.ProjectNumber}/{jobInfo.ExtractionName()}");
            streamWriter.WriteLine();

            foreach (FileAnonFailureInfo error in errors)
            {
                streamWriter.WriteLine($"-   {error.DicomFilePath}");
                streamWriter.WriteLine($"    {error.Status}");
                streamWriter.WriteLine($"    ```console");
                foreach (var line in error.StatusMessage.Split())
                {
                    streamWriter.WriteLine($"    {line}");
                }
                streamWriter.WriteLine($"    ```");
                streamWriter.WriteLine();
            }

            return true;
        }

        private void WriteVerificationFailuresCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string verificationFailuresPath = _fileSystem.Path.Combine(jobReportsDirAbsolute, "verification_failures.csv");
            var report = new FailureStoreReport(targetName: null, maxSize: 1_000);
            using var csvDest = new CsvDestination(null, verificationFailuresPath, addTimestampToFilename: false);
            report.AddDestination(csvDest);

            foreach (var fileVerificationFailureInfo in _jobStore.GetCompletedJobVerificationFailures(jobInfo.ExtractionJobIdentifier))
            {
                IEnumerable<Failure> fileFailures;
                try
                {
                    fileFailures = JsonConvert.DeserializeObject<IEnumerable<Failure>>(fileVerificationFailureInfo.Data);
                }
                catch (JsonException e)
                {
                    throw new ApplicationException("Could not deserialize report to IEnumerable<Failure>", e);
                }

                foreach (Failure failure in fileFailures)
                {
                    // NOTE(rkm 2022-03-17) Update the Resource to be the relative path in the output directory
                    failure.Resource = fileVerificationFailureInfo.AnonFilePath;

                    report.Add(failure);
                }
            }

            report.CloseReport();
        }

        private void WriteMissingFileList(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolut)
        {
            string proecssingErrorsPath = _fileSystem.Path.Combine(jobReportsDirAbsolut, "missing_files.csv");
            using var fileStream = _fileSystem.File.OpenWrite(proecssingErrorsPath);
            using var streamWriter = GetStreamWriter(fileStream);

            var missingFiles = _jobStore.GetCompletedJobMissingFileList(jobInfo.ExtractionJobIdentifier).ToList();

            if (!missingFiles.Any())
                return;

            streamWriter.WriteLine("MissingFilePath");

            // NOTE(rkm 2022-03-17) We're just writing a single field so no need to create a separate CSV record class
            foreach (var filePath in missingFiles)
                streamWriter.WriteLine(filePath);
        }

        private StreamWriter GetStreamWriter(Stream stream) => new(stream) { NewLine = _reportNewLine };

        private void WriteCsv<T>(Stream stream, IEnumerable<T> records) where T : IExtractionReportCsvRecord
        {
            using StreamWriter streamWriter = GetStreamWriter(stream);
            using var csvWriter = new CsvWriter(streamWriter, _csvConfiguration);

            csvWriter.WriteHeader<T>();
            csvWriter.NextRecord();

            csvWriter.WriteRecords(records);

            streamWriter.Flush();
        }
    }
}
