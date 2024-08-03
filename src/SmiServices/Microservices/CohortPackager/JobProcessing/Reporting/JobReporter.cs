using CsvHelper.Configuration;
using IsIdentifiable.Failures;
using IsIdentifiable.Options;
using IsIdentifiable.Reporting.Destinations;
using IsIdentifiable.Reporting.Reports;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;

namespace SmiServices.Microservices.CohortPackager.JobProcessing.Reporting
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

            var completedJobInfo = _jobStore.GetCompletedJobInfo(jobId);

            var jobReportsDirAbsolute = _fileSystem.Path.Combine(
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

            if (WriteProcessingErrorsCsv(completedJobInfo, jobReportsDirAbsolute))
                _logger.Warn($"Job {jobId} had errors during proecssing. Check {PROCESSING_ERRORS_FILE_NAME}");

            if (!completedJobInfo.IsIdentifiableExtraction)
                WriteVerificationFailuresCsv(completedJobInfo, jobReportsDirAbsolute);

            _logger.Info($"Reports for {jobId} written to {jobReportsDirAbsolute}");
        }

        private void WriteReadme(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            var lines = new List<string>
            {
                $"# SMI extraction validation report for {jobInfo.ProjectNumber} {jobInfo.ExtractionName()}",
                "",
                "Job info:",
                $"-   Job submitted at:             {jobInfo.JobSubmittedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {jobInfo.JobCompletedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {jobInfo.JobCompletedAt - jobInfo.JobSubmittedAt}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               {jobInfo.KeyTag}",
                $"-   Extraction modality:          {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"-   Requested identifier count:   {jobInfo.KeyValueCount}",
                $"-   User name:                    {jobInfo.UserName}",
                $"-   Identifiable extraction:      {(jobInfo.IsIdentifiableExtraction ? "Yes" : "No")}",
                $"-   Filtered extraction:          {(!jobInfo.IsNoFilterExtraction ? "Yes" : "No")}",
                ""
            };

            var jobReadmePath = _fileSystem.Path.Combine(jobReportsDirAbsolute, "README.md");
            using var fileStream = _fileSystem.File.OpenWrite(jobReadmePath);
            using var streamWriter = GetStreamWriter(fileStream);
            streamWriter.Write(string.Join(_reportNewLine, lines));
        }

        private readonly record struct DicomFileFailure(string DicomFilePath, string Reason);
        private bool WriteProcessingErrorsCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            var errorsPath = _fileSystem.Path.Combine(jobReportsDirAbsolute, PROCESSING_ERRORS_FILE_NAME);
            using var fileStream = _fileSystem.File.OpenWrite(errorsPath);
            using var streamWriter = GetStreamWriter(fileStream);
            using var csv = new CsvWriter(streamWriter, _csvConfiguration);

            var missing = _jobStore.GetCompletedJobMissingFileList(jobInfo.ExtractionJobIdentifier)
                .Select(static f => new DicomFileFailure(f, "Missing"));
            csv.WriteRecords(missing);

            if (!jobInfo.IsIdentifiableExtraction)
                csv.WriteRecords(_jobStore.GetCompletedJobAnonymisationFailures(jobInfo.ExtractionJobIdentifier)
                    .Select(static fi => new DicomFileFailure(fi.DicomFilePath, fi.Reason)));

            // Row == 2 => only header written => no failures recorded.
            return csv.Row > 2;
        }

        private readonly record struct Rejection(string ExtractionKey, int Count, string Reason);
        private void WriteRejectedFilesCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            var rejectionsReportPath = _fileSystem.Path.Combine(jobReportsDirAbsolute, "rejected_files.csv");
            using var fileStream = _fileSystem.File.OpenWrite(rejectionsReportPath);
            using var streamWriter = GetStreamWriter(fileStream);
            using var csv = new CsvWriter(streamWriter, _csvConfiguration);

            var jobRejections = _jobStore
                .GetCompletedJobRejections(jobInfo.ExtractionJobIdentifier)
                .OrderByDescending(static x => x.RejectionItems.Sum(static y => y.Value))
                .SelectMany(static info => info.RejectionItems.OrderByDescending(static x => x.Value)
                    .Select(reason => new Rejection(info.ExtractionIdentifier, reason.Value, reason.Key)));
            csv.WriteRecords(jobRejections);
        }

        private void WriteVerificationFailuresCsv(CompletedExtractJobInfo jobInfo, string jobReportsDirAbsolute)
        {
            var verificationFailuresReportName = "verification_failures";
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
                try
                {
                    // NOTE(rkm 2024-02-09) fileVerificationFailureInfo.Data can never be null, so neither can fileFailures
                    var fileFailures = JsonConvert.DeserializeObject<IEnumerable<Failure>>(fileVerificationFailureInfo.Data)!;
                    foreach (var failure in fileFailures)
                    {
                        // NOTE(rkm 2022-03-17) Updates the Resource to be the relative path in the output directory
                        failure.Resource = fileVerificationFailureInfo.AnonFilePath;

                        report.Add(failure);
                    }
                }
                catch (JsonException e)
                {
                    throw new ApplicationException($"Could not deserialize report content for {fileVerificationFailureInfo.AnonFilePath}", e);
                }

            }

            report.CloseReport();
        }

        private StreamWriter GetStreamWriter(Stream stream) => new(stream) { NewLine = _reportNewLine };
    }
}
