using Microservices.CohortPackager.Execution.ExtractJobStorage;


namespace Microservices.CohortPackager.Execution.JobProcessing
{
    public interface IJobCompleteNotifier
    {
        void NotifyJobCompleted(ExtractJobInfo jobInfo);
    }
}