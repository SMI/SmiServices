using SmiServices.Microservices.CohortPackager.ExtractJobStorage;


namespace SmiServices.Microservices.CohortPackager.JobProcessing.Notifying;

public interface IJobCompleteNotifier
{
    void NotifyJobCompleted(ExtractJobInfo jobInfo);
}
