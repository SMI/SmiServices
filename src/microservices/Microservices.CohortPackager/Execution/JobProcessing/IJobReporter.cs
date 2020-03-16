using System;

namespace Microservices.CohortPackager.Execution.JobProcessing
{
    public interface IJobReporter
    {
        void CreateReport(Guid jobId);
    }
}
