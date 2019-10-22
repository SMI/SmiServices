
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Smi.Common.Options;
using NLog;

namespace Microservices.CohortPackager.Execution.JobProcessing
{
    public class JobCompleteNotifier
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly CohortPackagerOptions _options;


        public JobCompleteNotifier(CohortPackagerOptions options)
        {
            _options = options;
        }


        public void NotifyJobCompleted(ExtractJobInfo jobInfo)
        {
            // This will be an email / RabbitMQ message in future
            _logger.Info("Job " + jobInfo.ExtractionJobIdentifier + " completed!");
        }
    }
}
