using System;
using System.Text;
using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NLog;


namespace Microservices.CohortPackager.Execution.JobProcessing
{
    /// <summary>
    /// Basic reporter which outputs to its logger. Should be used for testing only
    /// </summary>
    public class LoggingTextReporter : IJobReporter
    {
        private readonly ILogger _logger;

        private readonly IExtractJobStore _jobStore;


        public LoggingTextReporter([NotNull] IExtractJobStore jobStore)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
        }

        public void CreateReport(Guid jobId)
        {
            var sb = new StringBuilder();

            // TODO Possible common "generate header" function
            ExtractJobInfo jobInfo = _jobStore.GetCompletedJobInfo(jobId);
            sb.AppendLine();
            sb.AppendLine($"Extraction completion report for job {jobId}:");
            sb.AppendLine($"    Job submitted at:              {jobInfo.JobSubmittedAt}");
            sb.AppendLine($"    Project number:                {jobInfo.ProjectNumber}");
            sb.AppendLine($"    Extraction tag:                {jobInfo.KeyTag}");
            sb.AppendLine($"    Extraction modality:           {jobInfo.ExtractionModality ?? "Unspecified"}");
            sb.AppendLine($"    Requested identifier count:    {jobInfo.KeyValueCount}");
            sb.AppendLine();

            sb.AppendLine("Rejected files:");
            foreach ((string rejectReason, int count) in _jobStore.GetCompletedJobRejections(jobId))
                sb.AppendLine($"{rejectReason} x{count}");
            sb.AppendLine();

            sb.AppendLine("Anonymisation failures:");
            sb.AppendLine("Expected anonymised file | Failure reason");
            foreach ((string expectedAnonFile, string failureReason) in _jobStore.GetCompletedJobAnonymisationFailures(jobId))
                sb.AppendLine($"{expectedAnonFile} {failureReason}");
            sb.AppendLine();

            sb.AppendLine("Verification failures:");
            sb.AppendLine("Anonymised file | Failure reason");
            foreach ((string anonymisedFile, string failureReason) in _jobStore.GetCompletedJobVerificationFailures(jobId))
                sb.AppendLine($"{anonymisedFile} {failureReason}");
            sb.AppendLine();

            _logger.Info(sb.ToString);
        }
    }
}
