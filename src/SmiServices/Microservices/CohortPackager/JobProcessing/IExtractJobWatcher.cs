using System;

namespace SmiServices.Microservices.CohortPackager.JobProcessing;

public interface IExtractJobWatcher
{
    void ProcessJobs(Guid specificJob = new Guid());
}
