using System;


namespace SmiServices.Microservices.CohortPackager.JobProcessing.Reporting
{
    public interface IJobReporter
    {
        void CreateReports(Guid jobId);
    }
}
