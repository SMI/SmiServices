using System;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public interface IJobReporter
    {
        /// <summary>
        /// Create reports for the specified extraction job
        /// </summary>
        /// <param name="jobId"></param>
        void CreateReports(Guid jobId);
    }
}
