
using System;

namespace Microservices.CohortPackager.Execution.JobProcessing
{
    public interface IExtractJobWatcher
    {
        void ProcessJobs(Guid specificJob = new Guid());
    }
}
