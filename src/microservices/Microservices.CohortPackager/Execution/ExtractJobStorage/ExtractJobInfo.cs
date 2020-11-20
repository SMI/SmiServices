using JetBrains.Annotations;
using System;
using System.Text;

namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Class to wrap up all information about an extract job. Built by the <see cref="IExtractJobStore"/> when loading job information.
    /// </summary>
    public class ExtractJobInfo : IEquatable<ExtractJobInfo>
    {
        /// <summary>
        /// Unique identifier for this extract job. In the Mongo store implementation, this is also the _id of the document
        /// </summary>
        public Guid ExtractionJobIdentifier { get; }

        /// <summary>
        /// DateTime the job was submitted at (the time the ExtractorCL was run)
        /// </summary>
        public DateTime JobSubmittedAt { get; }

        /// <summary>
        /// Reference number for the project
        /// </summary>
        [NotNull]
        public string ProjectNumber { get; }

        /// <summary>
        /// Directory to extract files into, relative to the extraction root. Should be of the format projName/extractions/extractName
        /// </summary>
        [NotNull]
        public string ExtractionDirectory { get; }

        /// <summary>
        /// The DICOM tag of the identifier we are extracting (i.e. "SeriesInstanceUID")
        /// </summary>
        [NotNull]
        public string KeyTag { get; }

        /// <summary>
        /// Total number of expected distinct identifiers (i.e. number of distinct SeriesInstanceUIDs that are expected to be extracted)
        /// </summary>
        public uint KeyValueCount { get; }

        /// <summary>
        /// The modality being extracted
        /// </summary>
        [CanBeNull]
        public string ExtractionModality { get; }

        /// <summary>
        /// Current status of the extract job
        /// </summary>
        public ExtractJobStatus JobStatus { get; }

        public bool IsIdentifiableExtraction { get; }

        public bool IsNoFilterExtraction { get; }


        public ExtractJobInfo(
            Guid extractionJobIdentifier,
            DateTime jobSubmittedAt,
            [NotNull] string projectNumber,
            [NotNull] string extractionDirectory,
            [NotNull] string keyTag,
            uint keyValueCount,
            [CanBeNull] string extractionModality,
            ExtractJobStatus jobStatus,
            bool isIdentifiableExtraction,
            bool isNoFilterExtraction
            )
        {
            ExtractionJobIdentifier = (extractionJobIdentifier != default(Guid)) ? extractionJobIdentifier : throw new ArgumentNullException(nameof(extractionJobIdentifier));
            JobSubmittedAt = (jobSubmittedAt != default(DateTime)) ? jobSubmittedAt : throw new ArgumentNullException(nameof(jobSubmittedAt));
            ProjectNumber = (!string.IsNullOrWhiteSpace(projectNumber)) ? projectNumber : throw new ArgumentNullException(nameof(projectNumber));
            ExtractionDirectory = (!string.IsNullOrWhiteSpace(extractionDirectory)) ? extractionDirectory : throw new ArgumentNullException(nameof(extractionDirectory));
            KeyTag = (!string.IsNullOrWhiteSpace(keyTag)) ? keyTag : throw new ArgumentNullException(nameof(keyTag));
            KeyValueCount = (keyValueCount > 0) ? keyValueCount : throw new ArgumentNullException(nameof(keyValueCount));
            if (extractionModality != null)
                ExtractionModality = (!string.IsNullOrWhiteSpace(extractionModality)) ? extractionModality : throw new ArgumentNullException(nameof(extractionModality));
            JobStatus = (jobStatus != default(ExtractJobStatus)) ? jobStatus : throw new ArgumentException(nameof(jobStatus));
            IsIdentifiableExtraction = isIdentifiableExtraction;
            IsNoFilterExtraction = isNoFilterExtraction;
        }

        /// <summary>
        /// Returns the extraction name (last part of projName/extractions/extractName)
        /// </summary>
        /// <returns></returns>
        public string ExtractionName()
        {
            string[] split = ExtractionDirectory.Split('/', '\\');
            return split[^1];
        }
        
        /// <summary>
        /// Returns the project extraction directory (first two parts of projName/extractions/extractName)
        /// </summary>
        /// <returns></returns>
        public string ProjectExtractionDir()
        {
            int idx = ExtractionDirectory.LastIndexOfAny(new[] { '/', '\\' });
            return ExtractionDirectory.Substring(0, idx);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ExtractionJobIdentifier: " + ExtractionJobIdentifier);
            sb.AppendLine("JobStatus: " + JobStatus);
            sb.AppendLine("ExtractionDirectory: " + ExtractionDirectory);
            sb.AppendLine("KeyTag: " + KeyTag);
            sb.AppendLine("KeyCount: " + KeyValueCount);
            sb.AppendLine("ExtractionModality: " + ExtractionModality);
            sb.AppendLine("IsIdentifiableExtraction: " + IsIdentifiableExtraction);
            sb.AppendLine("IsNoFilterExtraction: " + IsNoFilterExtraction);
            return sb.ToString();
        }

        #region Equality Members

        public bool Equals(ExtractJobInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ExtractionJobIdentifier.Equals(other.ExtractionJobIdentifier) &&
                   JobSubmittedAt.Equals(other.JobSubmittedAt) &&
                   ProjectNumber == other.ProjectNumber &&
                   ExtractionDirectory == other.ExtractionDirectory &&
                   KeyTag == other.KeyTag &&
                   KeyValueCount == other.KeyValueCount &&
                   ExtractionModality == other.ExtractionModality &&
                   IsIdentifiableExtraction == other.IsIdentifiableExtraction &&
                   IsNoFilterExtraction == other.IsNoFilterExtraction &&
                   JobStatus == other.JobStatus;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ExtractJobInfo)obj);
        }

        public static bool operator ==(ExtractJobInfo left, ExtractJobInfo right) => Equals(left, right);

        public static bool operator !=(ExtractJobInfo left, ExtractJobInfo right) => !Equals(left, right);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ExtractionJobIdentifier.GetHashCode();
                hashCode = (hashCode * 397) ^ JobSubmittedAt.GetHashCode();
                hashCode = (hashCode * 397) ^ ProjectNumber.GetHashCode();
                hashCode = (hashCode * 397) ^ ExtractionDirectory.GetHashCode();
                hashCode = (hashCode * 397) ^ KeyTag.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) KeyValueCount;
                hashCode = (hashCode * 397) ^ (ExtractionModality != null ? ExtractionModality.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) JobStatus;
                hashCode = (hashCode * 397) ^ IsIdentifiableExtraction.GetHashCode();
                hashCode = (hashCode * 397) ^ IsNoFilterExtraction.GetHashCode();
                return hashCode;
            }
        }

        #endregion
    }
}
