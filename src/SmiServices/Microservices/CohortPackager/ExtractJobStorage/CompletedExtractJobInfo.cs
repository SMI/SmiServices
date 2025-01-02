using System;


namespace SmiServices.Microservices.CohortPackager.ExtractJobStorage;

public class CompletedExtractJobInfo : ExtractJobInfo
{
    /// <summary>
    /// DateTime the job was completed at (the time when CohortPackager ran its final checks)
    /// </summary>
    public DateTime JobCompletedAt { get; }


    public CompletedExtractJobInfo(
        Guid extractionJobIdentifier,
        DateTime jobSubmittedAt,
        DateTime completedAt,
        string projectNumber,
        string extractionDirectory,
        string keyTag,
        uint keyCount,
        string userName,
        string? extractionModality,
        bool isIdentifiableExtraction,
        bool isNoFilterExtraction
    )
        : base(
            extractionJobIdentifier,
            jobSubmittedAt,
            projectNumber,
            extractionDirectory,
            keyTag,
            keyCount,
            userName,
            extractionModality,
            ExtractJobStatus.Completed,
            isIdentifiableExtraction,
            isNoFilterExtraction
        )
    {
        JobCompletedAt = completedAt != default ? completedAt : throw new ArgumentException(null, nameof(completedAt));
    }
}
