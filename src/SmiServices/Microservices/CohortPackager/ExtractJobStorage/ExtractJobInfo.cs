using Equ;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SmiServices.Microservices.CohortPackager.ExtractJobStorage
{
    /// <summary>
    /// Class to wrap up all information about an extract job. Built by the <see cref="IExtractJobStore"/> when loading job information.
    /// </summary>
    public class ExtractJobInfo : MemberwiseEquatable<ExtractJobInfo>
    {
        /// <summary>
        /// Unique identifier for this extract job. In the Mongo store implementation, this is also the _id of the document
        /// </summary>
        public Guid ExtractionJobIdentifier { get; }

        /// <summary>
        /// DateTime the job was submitted at (the time the ExtractImages service was run)
        /// </summary>
        public DateTime JobSubmittedAt { get; }

        /// <summary>
        /// Reference number for the project
        /// </summary>
        public string ProjectNumber { get; }

        /// <summary>
        /// Directory to extract files into, relative to the extraction root. Should be of the format projName/extractions/extractName
        /// </summary>
        public string ExtractionDirectory { get; }

        /// <summary>
        /// The DICOM tag of the identifier we are extracting (i.e. "SeriesInstanceUID")
        /// </summary>
        public string KeyTag { get; }

        /// <summary>
        /// Total number of expected distinct identifiers (i.e. number of distinct SeriesInstanceUIDs that are expected to be extracted)
        /// </summary>
        public uint KeyValueCount { get; }

        /// <summary>
        /// Username of the person who submitted the job
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// The modality being extracted
        /// </summary>
        public string? ExtractionModality { get; }

        /// <summary>
        /// Current status of the extract job
        /// </summary>
        public ExtractJobStatus JobStatus { get; }

        public bool IsIdentifiableExtraction { get; }

        public bool IsNoFilterExtraction { get; }


        public ExtractJobInfo(
            Guid extractionJobIdentifier,
            DateTime jobSubmittedAt,
            string projectNumber,
            string extractionDirectory,
            string keyTag,
            uint keyValueCount,
            string userName,
            string? extractionModality,
            ExtractJobStatus jobStatus,
            bool isIdentifiableExtraction,
            bool isNoFilterExtraction
            )
        {
            ExtractionJobIdentifier = extractionJobIdentifier != default ? extractionJobIdentifier : throw new ArgumentOutOfRangeException(nameof(extractionJobIdentifier), $"Must not be the default {nameof(Guid)}");
            JobSubmittedAt = jobSubmittedAt != default ? jobSubmittedAt : throw new ArgumentOutOfRangeException(nameof(jobSubmittedAt), $"Must not be the default {nameof(DateTime)}");
            ProjectNumber = !string.IsNullOrWhiteSpace(projectNumber) ? projectNumber : throw new ArgumentOutOfRangeException(nameof(projectNumber), "Must not be null or whitespace");
            ExtractionDirectory = !string.IsNullOrWhiteSpace(extractionDirectory) ? extractionDirectory : throw new ArgumentOutOfRangeException(nameof(extractionDirectory), "Must not be null or whitespace");
            KeyTag = !string.IsNullOrWhiteSpace(keyTag) ? keyTag : throw new ArgumentOutOfRangeException(nameof(keyTag), "Must not be null or whitespace");
            KeyValueCount = keyValueCount > 0 ? keyValueCount : throw new ArgumentOutOfRangeException(nameof(keyValueCount), "Must not be zero");
            UserName = !string.IsNullOrWhiteSpace(userName) ? userName : throw new ArgumentOutOfRangeException(nameof(userName), "Must not be null or whitespace");
            if (extractionModality != null)
                ExtractionModality = !string.IsNullOrWhiteSpace(extractionModality) ? extractionModality : throw new ArgumentOutOfRangeException(nameof(extractionModality), "Must not be whitespace if passed");
            JobStatus = jobStatus != default ? jobStatus : throw new ArgumentOutOfRangeException(nameof(jobStatus), $"Must not be the default {nameof(ExtractJobStatus)}");
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

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ExtractionJobIdentifier: " + ExtractionJobIdentifier);
            sb.AppendLine("JobStatus: " + JobStatus);
            sb.AppendLine("ExtractionDirectory: " + ExtractionDirectory);
            sb.AppendLine("KeyTag: " + KeyTag);
            sb.AppendLine("KeyCount: " + KeyValueCount);
            sb.AppendLine("UserName: " + UserName);
            sb.AppendLine("ExtractionModality: " + ExtractionModality);
            sb.AppendLine("IsIdentifiableExtraction: " + IsIdentifiableExtraction);
            sb.AppendLine("IsNoFilterExtraction: " + IsNoFilterExtraction);
            return sb.ToString();
        }
    }
}
