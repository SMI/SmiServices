using NLog;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;

namespace SmiServices.Microservices.CohortPackager.JobProcessing.Notifying;

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
