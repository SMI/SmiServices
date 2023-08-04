using CsvHelper;
using CsvHelper.Configuration;
using IsIdentifiable.Reporting;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public abstract class JobReporterBase : IJobReporter, IDisposable
    {
        public readonly ReportFormat ReportFormat;

        public readonly string ReportNewLine;

        protected readonly ILogger Logger;

        private readonly IExtractJobStore _jobStore;

        private const string PixelDataStr = "PixelData";

        private readonly CsvConfiguration _csvConfiguration;


        protected JobReporterBase(
            IExtractJobStore jobStore,
            ReportFormat reportFormat,
            string? reportNewLine
        )
        {
            Logger = LogManager.GetLogger(GetType().Name);
            _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
            ReportFormat = (reportFormat == default) ? throw new ArgumentException(nameof(reportFormat)) : reportFormat;

            // NOTE(rkm 2020-11-20) IsNullOrWhiteSpace returns true for newline characters!
            if (!string.IsNullOrEmpty(reportNewLine))
            {
                if (reportNewLine.Contains(@"\"))
                    throw new ArgumentException("ReportNewLine contained an escaped backslash");

                ReportNewLine = reportNewLine;
            }
            else
            {
                // NOTE(rkm 2021-04-06) Escape the newline here so it prints correctly...
                Logger.Warn($"Not passed a specific newline string for creating reports. Defaulting to Environment.NewLine ('{Regex.Escape(Environment.NewLine)}')");
                // ... and just use the (unescaped) value as-is
                ReportNewLine = Environment.NewLine;
            }

            _csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                NewLine = ReportNewLine,
            };
        }

        public void CreateReport(Guid jobId)
        {
            CompletedExtractJobInfo jobInfo = _jobStore.GetCompletedJobInfo(jobId);
            Logger.Info($"Creating report(s) for {jobId}");

            if (ShouldWriteCombinedReport(jobInfo))
                WriteCombinedReport(jobInfo);
            else
                WriteSplitReport(jobInfo);

            Logger.Info($"Report(s) for {jobId} created");
        }

        private void WriteCombinedReport(CompletedExtractJobInfo jobInfo)
        {
            using Stream stream = GetStreamForSummary(jobInfo);
            using StreamWriter streamWriter = GetStreamWriter(stream);

            foreach (string line in JobHeader(jobInfo))
                streamWriter.WriteLine(line);

            streamWriter.WriteLine();
            streamWriter.WriteLine("Report contents:");

            // For identifiable extractions, write the metadata and list of missing files then return. The other parts don't make sense in this case
            if (jobInfo.IsIdentifiableExtraction)
            {
                streamWriter.WriteLine();
                streamWriter.WriteLine("-   Missing file list (files which were selected from an input ID but could not be found)");
                streamWriter.WriteLine();

                streamWriter.WriteLine("## Missing file list");
                streamWriter.WriteLine();
                WriteJobMissingFileList(streamWriter, _jobStore.GetCompletedJobMissingFileList(jobInfo.ExtractionJobIdentifier));
                streamWriter.WriteLine();
                streamWriter.WriteLine("--- end of report ---");
                streamWriter.Flush();
                FinishReportPart(stream);
                return;
            }

            streamWriter.WriteLine();
            streamWriter.WriteLine("-   Verification failures");
            streamWriter.WriteLine("    -   Summary");
            streamWriter.WriteLine("    -   Full Details");
            streamWriter.WriteLine("-   Blocked files");
            streamWriter.WriteLine("-   Anonymisation failures");
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Verification failures");
            streamWriter.WriteLine();
            WriteJobVerificationFailures(streamWriter, jobInfo.ExtractionJobIdentifier);
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Blocked files");
            streamWriter.WriteLine();
            foreach (ExtractionIdentifierRejectionInfo extractionIdentifierRejectionInfo in
                _jobStore.GetCompletedJobRejections(jobInfo.ExtractionJobIdentifier)
            )
                WriteJobRejections(streamWriter, extractionIdentifierRejectionInfo);
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Anonymisation failures");
            streamWriter.WriteLine();
            foreach (FileAnonFailureInfo fileAnonFailureInfo in
                _jobStore.GetCompletedJobAnonymisationFailures(jobInfo.ExtractionJobIdentifier)
            )
                WriteAnonFailure(streamWriter, fileAnonFailureInfo);
            streamWriter.WriteLine();

            streamWriter.WriteLine("--- end of report ---");

            streamWriter.Flush();
            FinishReportPart(stream);
        }

        /// <summary>
        /// Writes each part of the report content separately by calling the relevant GetStreamForX methods in turn
        /// </summary>
        /// <param name="jobInfo"></param>
        private void WriteSplitReport(CompletedExtractJobInfo jobInfo)
        {
            // TODO(rkm 2020-10-29) We can probably reduce the number of full collection enumerations in this method

            using (Stream stream = GetStreamForSummary(jobInfo))
            {
                using StreamWriter streamWriter = GetStreamWriter(stream);
                foreach (string line in JobHeader(jobInfo))
                    streamWriter.WriteLine(line);

                streamWriter.WriteLine();
                streamWriter.WriteLine("Files included:");
                streamWriter.WriteLine("-   README.md (this file)");
                streamWriter.WriteLine("-   pixel_data_summary.csv");
                streamWriter.WriteLine("-   pixel_data_full.csv");
                streamWriter.WriteLine("-   pixel_data_word_length_frequencies.csv");
                streamWriter.WriteLine("-   tag_data_summary.csv");
                streamWriter.WriteLine("-   tag_data_full.csv");
                streamWriter.WriteLine();
                streamWriter.WriteLine("This file contents:");
                streamWriter.WriteLine("-   Blocked files");
                streamWriter.WriteLine("-   Anonymisation failures");

                streamWriter.WriteLine();
                streamWriter.WriteLine("## Blocked files");
                streamWriter.WriteLine();
                IOrderedEnumerable<ExtractionIdentifierRejectionInfo> orderedRejections = _jobStore
                    .GetCompletedJobRejections(jobInfo.ExtractionJobIdentifier)
                    .OrderByDescending(x => x.RejectionItems.Sum(y => y.Value));
                foreach (ExtractionIdentifierRejectionInfo extractionIdentifierRejectionInfo in orderedRejections)
                    WriteJobRejections(streamWriter, extractionIdentifierRejectionInfo);

                streamWriter.WriteLine();
                streamWriter.WriteLine("## Anonymisation failures");
                streamWriter.WriteLine();
                foreach (FileAnonFailureInfo fileAnonFailureInfo in _jobStore.GetCompletedJobAnonymisationFailures(
                    jobInfo.ExtractionJobIdentifier))
                    WriteAnonFailure(streamWriter, fileAnonFailureInfo);

                streamWriter.WriteLine();
                streamWriter.WriteLine("--- end of report ---");

                streamWriter.Flush();
                FinishReportPart(stream);
            }

            // Local helper function to write each CSV
            void WriteCsv<T>(Stream stream, IEnumerable<T> records) where T : IExtractionReportCsvRecord
            {
                using StreamWriter streamWriter = GetStreamWriter(stream);
                using var csvWriter = new CsvWriter(streamWriter, _csvConfiguration);

                csvWriter.WriteHeader<T>();
                csvWriter.NextRecord();

                csvWriter.WriteRecords(records);

                streamWriter.Flush();
                FinishReportPart(stream);
            }

            // All validation failures for this job
            Dictionary<string, Dictionary<string, List<string>>> groupedFailures = GetJobVerificationFailures(jobInfo.ExtractionJobIdentifier);

            // First deal with the pixel data
            Dictionary<string, List<string>>? pixelFailures = groupedFailures.GetValueOrDefault(PixelDataStr);
            if (pixelFailures == null)
            {
                Logger.Info($"No {PixelDataStr} failures found for the extraction job");
                pixelFailures = new Dictionary<string, List<string>>();
            }

            // Create records for the pixel reports
            List<TagDataSummaryCsvRecord> pixelSummaryRecords = TagDataSummaryCsvRecord.BuildRecordList(PixelDataStr, pixelFailures).ToList();
            var wordLengthCounts = new Dictionary<uint, uint>();
            foreach (TagDataSummaryCsvRecord tagDataSummaryCsvRecord in pixelSummaryRecords)
            {
                var wordLen = (uint)tagDataSummaryCsvRecord.FailureValue.Length;
                if (!wordLengthCounts.ContainsKey(wordLen))
                    wordLengthCounts.Add(wordLen, 0);
                wordLengthCounts[wordLen] += (uint)tagDataSummaryCsvRecord.Occurrences;
                tagDataSummaryCsvRecord.RelativeFrequencyInReport = tagDataSummaryCsvRecord.RelativeFrequencyInTag;
            }

            // Write summary pixel CSV
            using (Stream stream = GetStreamForPixelDataSummary(jobInfo))
                WriteCsv(
                    stream,
                    pixelSummaryRecords
                        .OrderByDescending(x => x.FailureValue.Length)
                        .ThenByDescending(x => x.Occurrences)
                );

            // Write full pixel CSV
            using (Stream stream = GetStreamForPixelDataFull(jobInfo))
                WriteCsv(
                    stream,
                    TagDataFullCsvRecord
                        .BuildRecordList(PixelDataStr, pixelFailures)
                        .OrderByDescending(x => x.FailureValue.Length)
                );

            // Write the pixel text frequency file
            using (Stream stream = GetStreamForPixelDataWordLengthFrequencies(jobInfo))
                WriteCsv(
                    stream,
                    TagDataFrequencyRecord.BuildRecordList(wordLengthCounts)
                );

            // Now select all other tags
            Dictionary<string, Dictionary<string, List<string>>> otherTagFailures =
                groupedFailures
                    .Where(x => x.Key != PixelDataStr)
                    .ToDictionary(x => x.Key, x => x.Value);

            // Write the summary CSV for all other tags. Before doing so, we need to convert into records and calculate the relative frequencies
            var summaryRecordsByTag = new List<List<TagDataSummaryCsvRecord>>();
            var totalOccurrencesByValue = new Dictionary<string, uint>();
            foreach ((string tagName, Dictionary<string, List<string>> failures) in otherTagFailures)
            {
                List<TagDataSummaryCsvRecord> record = TagDataSummaryCsvRecord.BuildRecordList(tagName, failures).ToList();
                summaryRecordsByTag.Add(record);
                foreach (TagDataSummaryCsvRecord r in record)
                {
                    if (!totalOccurrencesByValue.ContainsKey(r.FailureValue))
                        totalOccurrencesByValue[r.FailureValue] = 0;
                    totalOccurrencesByValue[r.FailureValue] += r.Occurrences;
                }
            }
            var totalFailureValues = (uint)summaryRecordsByTag.Sum(x => x.Sum(y => y.Occurrences));
            var orderedTagSummaryRecords = new List<TagDataSummaryCsvRecord>();
            foreach (List<TagDataSummaryCsvRecord> tagRecordList in summaryRecordsByTag.OrderByDescending(x =>
                x.Sum(y => y.Occurrences)))
                foreach (TagDataSummaryCsvRecord record in tagRecordList.OrderByDescending(x => x.Occurrences))
                {
                    record.RelativeFrequencyInReport = totalOccurrencesByValue[record.FailureValue] * 1.0 / totalFailureValues;
                    orderedTagSummaryRecords.Add(record);
                }

            using (Stream stream = GetStreamForTagDataSummary(jobInfo))
                WriteCsv(
                    stream,
                    orderedTagSummaryRecords
                );

            // Write the full csv for all other tags.
            var fullRecordsByTag = new List<List<TagDataFullCsvRecord>>();
            foreach ((string tagName, Dictionary<string, List<string>> failures) in otherTagFailures)
                fullRecordsByTag.Add(TagDataFullCsvRecord.BuildRecordList(tagName, failures).ToList());
            var orderedFullTagRecords = new List<TagDataFullCsvRecord>();
            foreach (IEnumerable<TagDataFullCsvRecord> tagRecordSet in fullRecordsByTag.OrderBy(x => x[0].TagName))
                foreach (var x in tagRecordSet.OrderByDescending(x => x.FailureValue))
                    orderedFullTagRecords.Add(x);

            using (Stream stream = GetStreamForTagDataFull(jobInfo))
                WriteCsv(
                    stream,
                    orderedFullTagRecords
                );
        }

        protected abstract Stream GetStreamForSummary(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForPixelDataSummary(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForPixelDataFull(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForPixelDataWordLengthFrequencies(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForTagDataSummary(ExtractJobInfo jobInfo);
        protected abstract Stream GetStreamForTagDataFull(ExtractJobInfo jobInfo);

        protected abstract void FinishReportPart(Stream stream);

        private StreamWriter GetStreamWriter(Stream stream) => new(stream) { NewLine = ReportNewLine };

        private static IEnumerable<string> JobHeader(CompletedExtractJobInfo jobInfo)
        {
            string identExtraction = jobInfo.IsIdentifiableExtraction ? "Yes" : "No";
            string filteredExtraction = !jobInfo.IsNoFilterExtraction ? "Yes" : "No";

            return new List<string>
            {
                $"# SMI extraction validation report for {jobInfo.ProjectNumber}/{jobInfo.ExtractionName()}",
                "",
                "Job info:",
                $"-   Job submitted at:             {jobInfo.JobSubmittedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {jobInfo.JobCompletedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {(jobInfo.JobCompletedAt - jobInfo.JobSubmittedAt)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               {jobInfo.KeyTag}",
                $"-   Extraction modality:          {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"-   Requested identifier count:   {jobInfo.KeyValueCount}",
                $"-   Identifiable extraction:      {identExtraction}",
                $"-   Filtered extraction:          {filteredExtraction}",
            };
        }

        protected bool ShouldWriteCombinedReport(ExtractJobInfo jobInfo)
        {
            if (ReportFormat == ReportFormat.Combined || jobInfo.IsIdentifiableExtraction)
                return true;
            if (ReportFormat == ReportFormat.Split)
                return false;
            throw new ApplicationException($"No case for report format '{ReportFormat}'");
        }

        private static void WriteJobRejections(TextWriter streamWriter, ExtractionIdentifierRejectionInfo extractionIdentifierRejectionInfo)
        {
            streamWriter.WriteLine($"-   ID: {extractionIdentifierRejectionInfo.ExtractionIdentifier}");
            foreach ((string reason, int count) in extractionIdentifierRejectionInfo.RejectionItems.OrderByDescending(x => x.Value))
                streamWriter.WriteLine($"    -   {count}x '{reason}'");
        }

        private static void WriteAnonFailure(TextWriter streamWriter, FileAnonFailureInfo fileAnonFailureInfo)
        {
            streamWriter.WriteLine($"-   file '{fileAnonFailureInfo.ExpectedAnonFile}': '{fileAnonFailureInfo.Reason}'");
        }

        private Dictionary<string, Dictionary<string, List<string>>> GetJobVerificationFailures(Guid extractionJobIdentifier)
        {
            // For each problem field, we build a dict of problem values with a list of each file containing that value. This allows
            // grouping & ordering by occurrence

            // TODO Create a wrapper type for this(?)
            // Dict<TagName, Dict<FailureValue, List<Files>>>
            var groupedFailures = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (FileVerificationFailureInfo fileVerificationFailureInfo in _jobStore.GetCompletedJobVerificationFailures(extractionJobIdentifier))
            {
                IEnumerable<Failure>? fileFailures;
                try
                {
                    fileFailures = JsonConvert.DeserializeObject<IEnumerable<Failure>>(fileVerificationFailureInfo.Data);
                }
                catch (JsonException e)
                {
                    throw new ApplicationException("Could not deserialize report to IEnumerable<Failure>", e);
                }

                if(fileFailures == null)
                    throw new ApplicationException("Could not deserialize report to IEnumerable<Failure>");

                foreach (Failure failure in fileFailures)
                {
                    string tag = failure.ProblemField;
                    string value = failure.ProblemValue;

                    if (!groupedFailures.ContainsKey(tag))
                        groupedFailures.Add(tag, new Dictionary<string, List<string>>
                        {
                            { value, new List<string> { fileVerificationFailureInfo.AnonFilePath } },
                        });
                    else if (!groupedFailures[tag].ContainsKey(value))
                        groupedFailures[tag].Add(value, new List<string> { fileVerificationFailureInfo.AnonFilePath });
                    else
                        groupedFailures[tag][value].Add(fileVerificationFailureInfo.AnonFilePath);
                }
            }
            return groupedFailures;
        }

        private void WriteJobVerificationFailures(TextWriter streamWriter, Guid extractionJobIdentifier)
        {
            Dictionary<string, Dictionary<string, List<string>>> groupedFailures = GetJobVerificationFailures(extractionJobIdentifier);

            streamWriter.WriteLine("### Summary");
            streamWriter.WriteLine();

            var sb = new StringBuilder();

            // Write-out the groupings, ordered by descending count, as a summary without the list of associated files
            // Ignore the pixel data here since we deal with it separately below
            List<KeyValuePair<string, Dictionary<string, List<string>>>> grouped = groupedFailures
                .Where(x => x.Key != PixelDataStr)
                .OrderByDescending(x => x.Value.Sum(y => y.Value.Count))
                .ToList();

            foreach ((string tag, Dictionary<string, List<string>> failures) in grouped)
            {
                WriteVerificationValuesTag(tag, failures, streamWriter, sb);
                WriteVerificationValues(failures.OrderByDescending(x => x.Value.Count), streamWriter, sb);
            }

            // Now list the pixel data, which we instead order by decreasing length
            if (groupedFailures.TryGetValue(PixelDataStr, out Dictionary<string, List<string>>? pixelFailures))
            {
                WriteVerificationValuesTag(PixelDataStr, pixelFailures, streamWriter, sb);
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
                streamWriter.WriteLine($"-   {file}");
        }

        private void WriteVerificationValuesTag(string tag, Dictionary<string, List<string>> failures, TextWriter streamWriter, StringBuilder sb)
        {
            int totalOccurrences = failures.Sum(x => x.Value.Count);
            string line = $"-   Tag: {tag} ({totalOccurrences} total occurrence(s)){ReportNewLine}";
            streamWriter.Write(line);
            sb.Append(line);
        }

        private void WriteVerificationValues(IEnumerable<KeyValuePair<string, List<string>>> values, TextWriter streamWriter, StringBuilder sb)
        {

            foreach ((string problemVal, List<string> relatedFiles) in values)
            {
                string line = $"    -   Value: '{problemVal}' ({relatedFiles.Count} occurrence(s)){ReportNewLine}";
                streamWriter.Write(line);
                sb.Append(line);
                foreach (string file in relatedFiles)
                    sb.Append($"        -   {file}{ReportNewLine}");
            }

            streamWriter.WriteLine();
            sb.Append(ReportNewLine);
        }

        protected abstract void ReleaseUnmanagedResources();
        public abstract void Dispose();
        ~JobReporterBase() => ReleaseUnmanagedResources();
    }
}
