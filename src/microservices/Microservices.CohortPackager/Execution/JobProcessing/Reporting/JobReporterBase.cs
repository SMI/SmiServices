using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.IsIdentifiable.Reporting;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public abstract class JobReporterBase : IJobReporter, IDisposable
    {
        public readonly ReportFormat ReportFormat;

        [NotNull] protected readonly ILogger Logger;

        private readonly IExtractJobStore _jobStore;


        protected JobReporterBase(
            [NotNull] IExtractJobStore jobStore,
            ReportFormat reportFormat
        )
        {
            Logger = LogManager.GetLogger(GetType().Name);
            _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
            ReportFormat = reportFormat;
        }

        public void CreateReport(Guid jobId)
        {
            ExtractJobInfo jobInfo = _jobStore.GetCompletedJobInfo(jobId);

            using Stream stream = GetStreamForSummary(jobInfo);
            using var streamWriter = new StreamWriter(stream)
            {
                NewLine = "\r\n"
            };

            streamWriter.WriteLine();
            foreach (string line in JobHeader(jobInfo))
                streamWriter.WriteLine(line);
            streamWriter.WriteLine();

            // For identifiable extractions, write the metadata and list of missing files then return. The other parts don't make sense in this case
            if (jobInfo.IsIdentifiableExtraction)
            {
                streamWriter.WriteLine("## Missing file list");
                streamWriter.WriteLine();
                WriteJobMissingFileList(streamWriter, _jobStore.GetCompletedJobMissingFileList(jobInfo.ExtractionJobIdentifier));
                streamWriter.WriteLine();
                streamWriter.WriteLine("--- end of report ---");
                streamWriter.Flush();
                FinishReportPart(stream);
                return;
            }

            streamWriter.WriteLine("## Verification failures");
            streamWriter.WriteLine();
            WriteJobVerificationFailures(streamWriter,
                _jobStore.GetCompletedJobVerificationFailures(jobInfo.ExtractionJobIdentifier));
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Blocked files");
            streamWriter.WriteLine();
            foreach (ExtractionIdentifierRejectionInfo extractionIdentifierRejectionInfo in _jobStore.GetCompletedJobRejections(
                jobInfo.ExtractionJobIdentifier))
                WriteJobRejections(streamWriter, extractionIdentifierRejectionInfo);
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Anonymisation failures");
            streamWriter.WriteLine();
            foreach (FileAnonFailureInfo fileAnonFailureInfo in _jobStore.GetCompletedJobAnonymisationFailures(jobInfo.ExtractionJobIdentifier))
                WriteAnonFailure(streamWriter, fileAnonFailureInfo);
            streamWriter.WriteLine();

            streamWriter.WriteLine("--- end of report ---");

            streamWriter.Flush();

            FinishReportPart(stream);
        }

        /// <summary>
        /// Get the stream for writing the summary content to. This includes the job header and 
        /// </summary>
        /// <param name="jobInfo"></param>
        /// <returns></returns>
        protected abstract Stream GetStreamForSummary(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForPixelDataSummary(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForPixelDataFull(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForTagDataSummary(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForTagDataFull(ExtractJobInfo jobInfo);

        protected abstract void FinishReportPart(Stream stream);

        private static IEnumerable<string> JobHeader(ExtractJobInfo jobInfo)
        {
            string identExtraction = jobInfo.IsIdentifiableExtraction ? "Yes" : "No";
            string filteredExtraction = !jobInfo.IsNoFilterExtraction ? "Yes" : "No";
            var header = new List<string>
            {
                $"# SMI file extraction report for {jobInfo.ProjectNumber}",
                "",
                "Job info:",
                $"-    Job submitted at:              {jobInfo.JobSubmittedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-    Job extraction id:             {jobInfo.ExtractionJobIdentifier}",
                $"-    Extraction tag:                {jobInfo.KeyTag}",
                $"-    Extraction modality:           {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"-    Requested identifier count:    {jobInfo.KeyValueCount}",
                $"-    Identifiable extraction:       {identExtraction}",
                $"-    Filtered extraction:           {filteredExtraction}",
                "",
            };

            if (jobInfo.IsIdentifiableExtraction)
            {
                header.AddRange(new List<string>
                {
                    "Report contents:",
                    "-    Missing file list (files which were selected from an input ID but could not be found)",
                });
            }
            else
            {
                header.AddRange(new List<string>
                {
                    "Report contents:",
                    "-    Verification failures",
                    "    -    Summary",
                    "    -    Full Details",
                    "-    Blocked files",
                    "-    Anonymisation failures",
                });
            }

            return header;
        }

        private static void WriteJobRejections(TextWriter streamWriter, ExtractionIdentifierRejectionInfo extractionIdentifierRejectionInfo)
        {
            streamWriter.WriteLine($"- ID: {extractionIdentifierRejectionInfo.ExtractionIdentifier}");
            foreach ((string reason, int count) in extractionIdentifierRejectionInfo.RejectionItems.OrderByDescending(x => x.Value))
                streamWriter.WriteLine($"    - {count}x '{reason}'");
        }

        private static void WriteAnonFailure(TextWriter streamWriter, FileAnonFailureInfo fileAnonFailureInfo)
        {
            streamWriter.WriteLine($"- file '{fileAnonFailureInfo.ExpectedAnonFile}': '{fileAnonFailureInfo.Reason}'");
        }

        private static void WriteJobVerificationFailures(TextWriter streamWriter, IEnumerable<VerificationFailureInfo> verificationFailures)
        {
            // For each problem field, we build a dict of problem values with a list of each file containing that value. This allows
            // grouping & ordering by occurrence in the following section.
            var groupedFailures = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (VerificationFailureInfo verificationFailureInfo in verificationFailures)
            {
                IEnumerable<Failure> fileFailures;
                try
                {
                    fileFailures = JsonConvert.DeserializeObject<IEnumerable<Failure>>(verificationFailureInfo.Data);
                }
                catch (JsonException e)
                {
                    throw new ApplicationException("Could not deserialize report to IEnumerable<Failure>", e);
                }

                foreach (Failure failure in fileFailures)
                {
                    string tag = failure.ProblemField;
                    string value = failure.ProblemValue;

                    if (!groupedFailures.ContainsKey(tag))
                        groupedFailures.Add(tag, new Dictionary<string, List<string>>
                        {
                            { value, new List<string> { verificationFailureInfo.AnonFilePath } },
                        });
                    else if (!groupedFailures[tag].ContainsKey(value))
                        groupedFailures[tag].Add(value, new List<string> { verificationFailureInfo.AnonFilePath });
                    else
                        groupedFailures[tag][value].Add(verificationFailureInfo.AnonFilePath);
                }
            }

            streamWriter.WriteLine("### Summary");
            streamWriter.WriteLine();

            var sb = new StringBuilder();

            // Write-out the groupings, ordered by descending count, as a summary without the list of associated files
            // Ignore the pixel data here since we deal with it separately below
            const string pixelData = "PixelData";
            List<KeyValuePair<string, Dictionary<string, List<string>>>> grouped = groupedFailures
                .Where(x => x.Key != pixelData)
                .OrderByDescending(x => x.Value.Sum(y => y.Value.Count))
                .ToList();

            foreach ((string tag, Dictionary<string, List<string>> failures) in grouped)
            {
                WriteVerificationValuesTag(tag, failures, streamWriter, sb);
                WriteVerificationValues(failures.OrderByDescending(x => x.Value.Count), streamWriter, sb);
            }

            // Now list the pixel data, which we instead order by decreasing length
            if (groupedFailures.TryGetValue(pixelData, out Dictionary<string, List<string>> pixelFailures))
            {
                WriteVerificationValuesTag(pixelData, pixelFailures, streamWriter, sb);
                WriteVerificationValues(pixelFailures.OrderByDescending(x => x.Key.Length), streamWriter, sb);
            }

            // Now write-out the same, but with the file listing
            streamWriter.WriteLine();
            streamWriter.WriteLine("### Full details");
            streamWriter.WriteLine();
            streamWriter.Write(sb);
        }

        private static void WriteJobMissingFileList(TextWriter streamWriter, IEnumerable<string> missingFiles)
        {
            foreach (string file in missingFiles)
                streamWriter.WriteLine($"-    {file}");
        }

        private static void WriteVerificationValuesTag(string tag, Dictionary<string, List<string>> failures, TextWriter streamWriter, StringBuilder sb)
        {
            int totalOccurrences = failures.Sum(x => x.Value.Count);
            string line = $"- Tag: {tag} ({totalOccurrences} total occurrence(s))";
            streamWriter.WriteLine(line);
            sb.AppendLine(line);
        }

        private static void WriteVerificationValues(IEnumerable<KeyValuePair<string, List<string>>> values, TextWriter streamWriter, StringBuilder sb)
        {

            foreach ((string problemVal, List<string> relatedFiles) in values)
            {
                string line = $"    - Value: '{problemVal}' ({relatedFiles.Count} occurrence(s))";
                streamWriter.WriteLine(line);
                sb.AppendLine(line);
                foreach (string file in relatedFiles)
                    sb.AppendLine($"        - {file}");
            }

            streamWriter.WriteLine();
            sb.AppendLine();
        }

        protected abstract void ReleaseUnmanagedResources();
        public abstract void Dispose();
        ~JobReporterBase() => ReleaseUnmanagedResources();
    }
}
