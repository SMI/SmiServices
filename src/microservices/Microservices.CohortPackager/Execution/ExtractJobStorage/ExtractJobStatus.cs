
namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Possible job statuses used by <see cref="ExtractJobInfo"/>
    /// </summary>
    public enum ExtractJobStatus
    {
        Unknown,
        WaitingForCollectionInfo,
        WaitingForStatuses,
        ReadyForChecks,
        Completed,
        Failed,
    }
}
