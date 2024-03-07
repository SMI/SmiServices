using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NLog;

namespace Microservices.CohortPackager.Execution.JobProcessing.Notifying
{
    /// <summary>
    /// Basic notifier which outputs to its logger. Should be used for testing only
    /// </summary>
    public class LoggingNotifier : IJobCompleteNotifier
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void NotifyJobCompleted(ExtractJobInfo jobInfo)
        {
            _logger.Info("Job " + jobInfo.ExtractionJobIdentifier + " completed!");
        }
    }
}
