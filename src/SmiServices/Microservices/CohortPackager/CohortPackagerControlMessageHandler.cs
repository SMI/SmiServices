
using NLog;
using System;
using Smi.Common.Messaging;
using SmiServices.Microservices.CohortPackager.JobProcessing;

namespace SmiServices.Microservices.CohortPackager
{
    public class CohortPackagerControlMessageHandler : IControlMessageHandler
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IExtractJobWatcher _jobWatcher;


        public CohortPackagerControlMessageHandler(IExtractJobWatcher jobWatcher)
        {
            _jobWatcher = jobWatcher;
        }

        public void ControlMessageHandler(string action, string? message = null)
        {
            _logger.Info("Received control event with action: " + action + " and message: " + (message ?? ""));

            // Only have 1 case to handle here
            if (action != "processjobs")
                return;

            _logger.Info("Received request to process jobs now");

            Guid toProcess = default;

            if (message != null)
            {
                if (!Guid.TryParse(message, out toProcess))
                {
                    _logger.Warn("Could not parse \"" + message + "\" to a job GUID");
                    return;
                }

                _logger.Info("Calling process for job " + toProcess);
            }
            else
            {
                _logger.Info("No message content, doing process for all jobs");
            }

            _jobWatcher.ProcessJobs(toProcess);
        }
    }
}
