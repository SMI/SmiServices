using Microservices.CohortPackager.Execution.ExtractJobStorage;


namespace Microservices.CohortPackager.Execution.JobProcessing.Notifying
{
    public interface IJobCompleteNotifier
    {
        void NotifyJobCompleted(ExtractJobInfo jobInfo);
    }
}
