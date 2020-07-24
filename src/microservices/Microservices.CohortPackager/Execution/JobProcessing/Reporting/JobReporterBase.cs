using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.IsIdentifiable.Reporting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Microservices.IsIdentifiable.Failures;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public abstract class JobReporterBase : IJobReporter, IDisposable
    {
        private readonly IExtractJobStore _jobStore;

        protected JobReporterBase(
            [NotNull] IExtractJobStore jobStore,
            [CanBeNull] string _ // NOTE(rkm 2020-03-17) Required to force matching constructors for all derived types for construction via reflection
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

            streamWriter.WriteLine("## Verification failures");
            streamWriter.WriteLine();
            foreach ((string anonymisedFile, string failureData) in _jobStore.GetCompletedJobVerificationFailures(jobInfo.ExtractionJobIdentifier))
                WriteJobVerificationFailures(streamWriter, anonymisedFile, failureData);
            streamWriter.WriteLine();

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
                $"    Job submitted at:              {jobInfo.JobSubmittedAt}",
                $"    Job extraction id:             {jobInfo.ExtractionJobIdentifier}",
                $"    Extraction tag:                {jobInfo.KeyTag}",
                $"    Extraction modality:           {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"    Requested identifier count:    {jobInfo.KeyValueCount}",
            };

        private static void WriteJobRejections(TextWriter streamWriter, Tuple<string, Dictionary<string, int>> rejection)
        {
            (string rejectionKey, Dictionary<string, int> rejectionItems) = rejection;
            streamWriter.WriteLine($"- ID '{rejectionKey}':");
            foreach((string reason, int count) in rejectionItems)
                streamWriter.WriteLine($"    - {count}x '{reason}'");
        }

        private static void WriteAnonFailure(TextWriter streamWriter, string expectedAnonFile, string failureReason)
        {
            streamWriter.WriteLine($"- file '{expectedAnonFile}': '{failureReason}'");
        }

        private static void WriteJobVerificationFailures(TextWriter streamWriter, string anonymisedFile, string failureData)
        {
            IEnumerable<Failure> failures;
            try
            {
                failures = JsonConvert.DeserializeObject<IEnumerable<Failure>>(failureData);
            }
            catch (JsonException e)
            {
                throw new ApplicationException("Could not deserialize report to IEnumerable<Failure>", e);
            }

            streamWriter.WriteLine($"- file '{anonymisedFile}':");
            foreach (Failure failure in failures)
            {
                streamWriter.WriteLine("      (Problem Field | Problem Value)");
                streamWriter.WriteLine($"    - {failure.ProblemField} | {failure.ProblemValue}");
                streamWriter.WriteLine("         (Classification | Offset | Word)");
                foreach (FailurePart part in failure.Parts)
                {
                    streamWriter.WriteLine($"       - {part.Classification} | {part.Offset} | {part.Word}");
                }
            }
            streamWriter.WriteLine();
        }

        protected abstract void ReleaseUnmanagedResources();
        public abstract void Dispose();
        ~JobReporterBase() => ReleaseUnmanagedResources();
    }
}
