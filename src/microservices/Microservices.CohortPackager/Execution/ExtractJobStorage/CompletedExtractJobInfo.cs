using System;
using System.Diagnostics.CodeAnalysis;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    public class CompletedExtractJobInfo : ExtractJobInfo, IEquatable<CompletedExtractJobInfo>
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

        #region  Equality Members

        public bool Equals(CompletedExtractJobInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && JobCompletedAt.Equals(other.JobCompletedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompletedExtractJobInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), JobCompletedAt);
        }

        public static bool operator ==(CompletedExtractJobInfo left, CompletedExtractJobInfo right) => Equals(left, right);

        public static bool operator !=(CompletedExtractJobInfo left, CompletedExtractJobInfo right) => !Equals(left, right);

        #endregion
    }
}
