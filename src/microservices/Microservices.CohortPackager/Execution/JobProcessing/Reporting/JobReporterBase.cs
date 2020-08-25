using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.IsIdentifiable.Reporting;
using Newtonsoft.Json;
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
        private readonly IExtractJobStore _jobStore;

        protected JobReporterBase(
            [NotNull] IExtractJobStore jobStore,
            // NOTE(rkm 2020-03-17) Required to force matching constructors for all derived types for construction via reflection
            // ReSharper disable once UnusedParameter.Local
            [CanBeNull] string _
        )
        {
            _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
        }

        public void CreateReport(Guid jobId)
        {
            ExtractJobInfo jobInfo = _jobStore.GetCompletedJobInfo(jobId);

            using Stream stream = GetStream(jobId);
            using var streamWriter = new StreamWriter(stream);

            streamWriter.WriteLine();
            foreach (string line in JobHeader(jobInfo))
                streamWriter.WriteLine(line);
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Verification failures");
            streamWriter.WriteLine();
            WriteJobVerificationFailures(streamWriter, _jobStore.GetCompletedJobVerificationFailures(jobInfo.ExtractionJobIdentifier));
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Rejected files");
            streamWriter.WriteLine();
            foreach (Tuple<string, Dictionary<string, int>> rejection in _jobStore.GetCompletedJobRejections(jobInfo.ExtractionJobIdentifier))
                WriteJobRejections(streamWriter, rejection);
            streamWriter.WriteLine();

            streamWriter.WriteLine("## Anonymisation failures");
            streamWriter.WriteLine();
            foreach ((string expectedAnonFile, string failureReason) in _jobStore.GetCompletedJobAnonymisationFailures(jobInfo.ExtractionJobIdentifier))
                WriteAnonFailure(streamWriter, expectedAnonFile, failureReason);
            streamWriter.WriteLine();

            streamWriter.WriteLine("--- end of report ---");

            streamWriter.Flush();

            stream.Position = 0;
            FinishReport(stream);
        }

        protected abstract Stream GetStream(Guid jobId);

        protected abstract void FinishReport(Stream stream);

        private static IEnumerable<string> JobHeader(ExtractJobInfo jobInfo)
            => new[]
            {
                $"# SMI file extraction report for {jobInfo.ProjectNumber}",
                "",
                "Job info:",
                $"-    Job submitted at:              {jobInfo.JobSubmittedAt.ToString("s", CultureInfo.InvariantCulture)}",
                $"-    Job extraction id:             {jobInfo.ExtractionJobIdentifier}",
                $"-    Extraction tag:                {jobInfo.KeyTag}",
                $"-    Extraction modality:           {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"-    Requested identifier count:    {jobInfo.KeyValueCount}",
                "",
                "Report contents:",
                "-    Verification failures",
                "    -    Summary",
                "    -    Full Details",
                "-    Rejected failures",
                "-    Anonymisation failures",
            };

        private static void WriteJobRejections(TextWriter streamWriter, Tuple<string, Dictionary<string, int>> rejection)
        {
            (string rejectionKey, Dictionary<string, int> rejectionItems) = rejection;
            streamWriter.WriteLine($"- ID: {rejectionKey}");
            foreach ((string reason, int count) in rejectionItems.OrderByDescending(x => x.Value))
                streamWriter.WriteLine($"    - {count}x '{reason}'");
        }

        private static void WriteAnonFailure(TextWriter streamWriter, string expectedAnonFile, string failureReason)
        {
            streamWriter.WriteLine($"- file '{expectedAnonFile}': '{failureReason}'");
        }

        private static void WriteJobVerificationFailures(TextWriter streamWriter, IEnumerable<Tuple<string, string>> verificationFailures)
        {
            // For each problem field, we build a dict of problem values with a list of each file containing that value. This allows
            // grouping & ordering by occurrence in the following section.
            var groupedFailures = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach ((string anonFile, string failureData) in verificationFailures)
            {
                IEnumerable<Failure> fileFailures;
                try
                {
                    fileFailures = JsonConvert.DeserializeObject<IEnumerable<Failure>>(failureData);
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
                            { value, new List<string> { anonFile } },
                        });
                    else if (!groupedFailures[tag].ContainsKey(value))
                        groupedFailures[tag].Add(value, new List<string> { anonFile });
                    else
                        groupedFailures[tag][value].Add(anonFile);
                }
            }

            streamWriter.WriteLine("### Summary");
            streamWriter.WriteLine();

            var sb = new StringBuilder();

            // Write-out the groupings, ordered by descending count, as a summary without the list of associated files
            List<KeyValuePair<string, Dictionary<string, List<string>>>> grouped = groupedFailures.OrderByDescending(x => x.Value.Sum(y => y.Value.Count)).ToList();
            foreach ((string tag, Dictionary<string, List<string>> failures) in grouped)
            {
                int totalOccurrences = failures.Sum(x => x.Value.Count);
                string line = $"- Tag: {tag} ({totalOccurrences} total occurrence(s))";
                streamWriter.WriteLine(line);
                sb.AppendLine(line);
                foreach ((string problemVal, List<string> relatedFiles) in failures.OrderByDescending(x => x.Value.Count))
                {
                    line = $"    - Value: '{problemVal}' ({relatedFiles.Count} occurrence(s))";
                    streamWriter.WriteLine(line);
                    sb.AppendLine(line);
                    foreach (string file in relatedFiles)
                        sb.AppendLine($"        - {file}");
                }

                streamWriter.WriteLine();
                sb.AppendLine();
            }

            // Now write-out the same, but with the file listing
            streamWriter.WriteLine();
            streamWriter.WriteLine("### Full details");
            streamWriter.WriteLine();
            streamWriter.Write(sb);
        }

        protected abstract void ReleaseUnmanagedResources();
        public abstract void Dispose();
        ~JobReporterBase() => ReleaseUnmanagedResources();
    }
}
