
namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Possible job statuses used by <see cref="ExtractJobInfo"/>
    /// </summary>
    public enum ExtractJobStatus
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// 
        /// </summary>
        WaitingForJobInfo,

        /// <summary>
        /// 
        /// </summary>
        WaitingForCollectionInfo,

        /// <summary>
        /// 
        /// </summary>
        WaitingForStatuses,
        
        /// <summary>
        /// 
        /// </summary>
        Archived
    }
}
