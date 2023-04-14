using System;
using System.Diagnostics.CodeAnalysis;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
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
            [NotNull] string projectNumber,
            [NotNull] string extractionDirectory,
            [NotNull] string keyTag,
            uint keyCount,
            [NotNull] string extractionModality,
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
                extractionModality,
                ExtractJobStatus.Completed,
                isIdentifiableExtraction,
                isNoFilterExtraction
            )
        {
            JobCompletedAt = (completedAt != default) ? completedAt : throw new ArgumentException(nameof(completedAt));
        }
    }
}
