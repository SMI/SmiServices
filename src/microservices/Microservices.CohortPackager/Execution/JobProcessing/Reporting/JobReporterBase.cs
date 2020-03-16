using System;
using System.Collections.Generic;
using System.IO;
using Microservices.CohortPackager.Execution.ExtractJobStorage;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public abstract class JobReporterBase : IJobReporter, IDisposable
    {
        private readonly IExtractJobStore _jobStore;

        protected JobReporterBase(IExtractJobStore jobStore)
        {
            _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
        }

        public void CreateReport(Guid jobId)
        {
            ExtractJobInfo jobInfo = _jobStore.GetCompletedJobInfo(jobId);

            using (Stream stream = GetStream(jobId))
            {
                var streamWriter = new StreamWriter(stream);

                streamWriter.WriteLine();
                foreach (string line in JobHeader(jobInfo))
                    streamWriter.WriteLine(line);
                streamWriter.WriteLine();

                streamWriter.WriteLine("Rejected files:");
                foreach ((string rejectReason, int count) in _jobStore.GetCompletedJobRejections(jobInfo.ExtractionJobIdentifier))
                    streamWriter.WriteLine($"{rejectReason} x{count}");
                streamWriter.WriteLine();

                streamWriter.WriteLine("Anonymisation failures:");
                streamWriter.WriteLine("Expected anonymised file | Failure reason");
                foreach ((string expectedAnonFile, string failureReason) in _jobStore.GetCompletedJobAnonymisationFailures(jobInfo.ExtractionJobIdentifier))
                    streamWriter.WriteLine($"{expectedAnonFile} {failureReason}");
                streamWriter.WriteLine();

                streamWriter.WriteLine("Verification failures:");
                streamWriter.WriteLine("Anonymised file | Failure reason");
                foreach ((string anonymisedFile, string failureReason) in _jobStore.GetCompletedJobVerificationFailures(jobInfo.ExtractionJobIdentifier))
                    streamWriter.WriteLine($"{anonymisedFile} {failureReason}");
                streamWriter.WriteLine();

                streamWriter.Flush();

                stream.Position = 0;
                FinishReport(stream);
            }
        }

        protected abstract Stream GetStream(Guid jobId);

        protected abstract void FinishReport(Stream stream);

        private static IEnumerable<string> JobHeader(ExtractJobInfo jobInfo)
            => new[]
            {
                $"Extraction completion report for job {jobInfo.ExtractionJobIdentifier}:",
                $"    Job submitted at:              {jobInfo.JobSubmittedAt}",
                $"    Project number:                {jobInfo.ProjectNumber}",
                $"    Extraction tag:                {jobInfo.KeyTag}",
                $"    Extraction modality:           {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"    Requested identifier count:    {jobInfo.KeyValueCount}",
            };

        protected abstract void ReleaseUnmanagedResources();
        public abstract void Dispose();
        ~JobReporterBase() => ReleaseUnmanagedResources();
    }
}
