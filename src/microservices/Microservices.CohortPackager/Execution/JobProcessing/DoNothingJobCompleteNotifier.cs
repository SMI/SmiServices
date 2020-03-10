
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NLog;

namespace Microservices.CohortPackager.Execution.JobProcessing
{
    public class DoNothingJobCompleteNotifier : IJobCompleteNotifier
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void NotifyJobCompleted(ExtractJobInfo jobInfo)
        {
            _logger.Info("Job " + jobInfo.ExtractionJobIdentifier + " completed!");
        }
    }
}
