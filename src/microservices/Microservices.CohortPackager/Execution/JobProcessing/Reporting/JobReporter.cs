using CsvHelper.Configuration;
using IsIdentifiable.Failures;
using IsIdentifiable.Options;
using IsIdentifiable.Reporting.Destinations;
using IsIdentifiable.Reporting.Reports;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
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
    public sealed class JobReporter : IJobReporter
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _reportNewLine;
        private readonly IExtractJobStore _jobStore;
        private readonly IFileSystem _fileSystem;
        private readonly string _extractionRoot;
        private readonly CsvConfiguration _csvConfiguration;
        private const string PROCESSING_ERRORS_FILE_NAME = "processing_errors.csv";


        public JobReporter(
            IExtractJobStore jobStore,
            IFileSystem fileSystem,
            string extractionRoot,
            string? reportNewLine
        )
        {
            _jobStore = jobStore;
            _fileSystem = fileSystem;
            _extractionRoot = extractionRoot;

            if (!_fileSystem.Path.IsPathRooted(extractionRoot))
                throw new ArgumentException("Path must be rooted", nameof(extractionRoot));

            // NOTE(rkm 2020-11-20) IsNullOrWhiteSpace returns true for newline characters!
            if (!string.IsNullOrEmpty(reportNewLine))
            {
                if (reportNewLine != "\n" && reportNewLine != "\r\n")
                    throw new ArgumentOutOfRangeException(nameof(reportNewLine), "Must be a Unix or Windows newline");
                _reportNewLine = reportNewLine;
            }
            else
            {
                // NOTE(rkm 2021-04-06) Escape the newline here so it prints correctly...
                _logger.Warn($"Not passed a specific newline string for creating reports. Defaulting to Environment.NewLine ('{Regex.Escape(Environment.NewLine)}')");
                // ... and just use the (unescaped) value as-is
                _reportNewLine = Environment.NewLine;
            }

            _csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                NewLine = _reportNewLine,
            };
        }

        public void CreateReports(Guid jobId)
        {
            if (jobId == default)
                throw new ArgumentOutOfRangeException(nameof(jobId), "Must provide a non-zero jobId");

            _logger.Info($"Creating reports for {jobId}");

            CompletedExtractJobInfo completedJobInfo = _jobStore.GetCompletedJobInfo(jobId);

            string jobReportsDirAbsolute = _fileSystem.Path.Combine(
                _extractionRoot,
                completedJobInfo.ProjectExtractionDir(),
                "reports",
                completedJobInfo.ExtractionName()
            );

            if (_fileSystem.Directory.Exists(jobReportsDirAbsolute))
                throw new ApplicationException($"Job reports directory already exists: {jobReportsDirAbsolute}");

            _fileSystem.Directory.CreateDirectory(jobReportsDirAbsolute);

            WriteReadme(completedJobInfo, jobReportsDirAbsolute);

            WriteRejectedFilesCsv(completedJobInfo, jobReportsDirAbsolute);

            var hadErrors = WriteProcessingErrorsCsv(completedJobInfo, jobReportsDirAbsolute);
            if (hadErrors)
            {
                _logger.Warn($"Job {jobId} had errors during proecssing. Check {PROCESSING_ERRORS_FILE_NAME}");
            }

            if (completedJobInfo.IsIdentifiableExtraction)
                return;

            WriteVerificationFailuresCsv(completedJobInfo, jobReportsDirAbsolute);

            _logger.Info($"Reports for {jobId} written to {jobReportsDirAbsolute}");
        }

        private void WriteReadme(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string identExtraction = jobInfo.IsIdentifiableExtraction ? "Yes" : "No";
            string filteredExtraction = !jobInfo.IsNoFilterExtraction ? "Yes" : "No";
            var lines = new List<string>
            {
                $"# SMI extraction validation report for {jobInfo.ProjectNumber} {jobInfo.ExtractionName()}",
                "",
                "Job info:",
                $"-   Job submitted at:             {jobInfo.JobSubmittedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {jobInfo.JobCompletedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {(jobInfo.JobCompletedAt - jobInfo.JobSubmittedAt)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               {jobInfo.KeyTag}",
                $"-   Extraction modality:          {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"-   Requested identifier count:   {jobInfo.KeyValueCount}",
                $"-   User name:                    {jobInfo.UserName}",
                $"-   Identifiable extraction:      {identExtraction}",
                $"-   Filtered extraction:          {filteredExtraction}",
            };

            string jobReadmePath = _fileSystem.Path.Combine(jobReportsDirAbsolute, "README.md");
            using var fileStream = _fileSystem.File.OpenWrite(jobReadmePath);
            using var streamWriter = GetStreamWriter(fileStream);

            foreach (string line in lines)
                streamWriter.WriteLine(line);
        }

        private bool WriteProcessingErrorsCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string errorsPath = _fileSystem.Path.Combine(jobReportsDirAbsolute, PROCESSING_ERRORS_FILE_NAME);
            using var fileStream = _fileSystem.File.OpenWrite(errorsPath);
            using var streamWriter = GetStreamWriter(fileStream);

            streamWriter.WriteLine("DicomFilePath,Reason");

            var hasFailures = false;

            var missingFiles = _jobStore.GetCompletedJobMissingFileList(jobInfo.ExtractionJobIdentifier);
            foreach (var filePath in missingFiles)
            {
                streamWriter.WriteLine($"{filePath},Missing");
                hasFailures = true;
            }

            if (jobInfo.IsIdentifiableExtraction)
                return hasFailures;

            var anonFailures = _jobStore.GetCompletedJobAnonymisationFailures(jobInfo.ExtractionJobIdentifier);
            foreach (var failureInfo in anonFailures)
            {
                streamWriter.WriteLine($"{failureInfo.DicomFilePath},{failureInfo.Reason}");
                hasFailures = true;
            }

            return hasFailures;
        }

        private void WriteRejectedFilesCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string rejectionsReportPath = _fileSystem.Path.Combine(jobReportsDirAbsolute, "rejected_files.csv");
            using var fileStream = _fileSystem.File.OpenWrite(rejectionsReportPath);
            using var streamWriter = GetStreamWriter(fileStream);

            streamWriter.WriteLine("ExtractionKey,Count,Reason");

            var jobRejections = _jobStore
                .GetCompletedJobRejections(jobInfo.ExtractionJobIdentifier)
                .OrderByDescending(x => x.RejectionItems.Sum(y => y.Value));

            foreach (var rejectionInfo in jobRejections)
                foreach (var rejectionReason in rejectionInfo.RejectionItems.OrderByDescending(x => x.Value))
                    streamWriter.WriteLine($"{rejectionInfo.ExtractionIdentifier},{rejectionReason.Value},{rejectionReason.Key}");
        }

        private void WriteVerificationFailuresCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            string verificationFailuresReportName = "verification_failures";
            var report = new FailureStoreReport(targetName: "", maxSize: 1_000, _fileSystem);

            // TODO(rkm 2022-03-22) Can we pass this directly?
            var isIdentOptions = new IsIdentifiableFileOptions
            {
                DestinationCsvFolder = jobReportsDirAbsolute
            };

            using var csvDest = new CsvDestination(isIdentOptions, verificationFailuresReportName, _fileSystem, false, _csvConfiguration);
            report.AddDestination(csvDest);

            foreach (var fileVerificationFailureInfo in _jobStore.GetCompletedJobVerificationFailures(jobInfo.ExtractionJobIdentifier))
            {
                IEnumerable<Failure>? fileFailures;
                try
                {
                    // NOTE(rkm 2024-02-09) fileVerificationFailureInfo.Data can never be null, so neither can fileFailures
                    fileFailures = JsonConvert.DeserializeObject<IEnumerable<Failure>>(fileVerificationFailureInfo.Data)!;
                }
                catch (JsonException e)
                {
                    throw new ApplicationException($"Could not deserialize report content for {fileVerificationFailureInfo.AnonFilePath}", e);
                }

                foreach (Failure failure in fileFailures)
                {
                    // NOTE(rkm 2022-03-17) Updates the Resource to be the relative path in the output directory
                    failure.Resource = fileVerificationFailureInfo.AnonFilePath;

                    report.Add(failure);
                }
            }

            report.CloseReport();
        }

        private StreamWriter GetStreamWriter(Stream stream) => new(stream) { NewLine = _reportNewLine };
    }
}
