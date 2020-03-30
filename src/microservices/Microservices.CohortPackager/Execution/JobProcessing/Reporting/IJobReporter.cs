using System;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public interface IJobReporter
    {
        void CreateReport(Guid jobId);
    }
}
