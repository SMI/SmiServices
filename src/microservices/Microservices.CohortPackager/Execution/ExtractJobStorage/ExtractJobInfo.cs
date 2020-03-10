using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Smi.Common.Messages.Extraction;

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
        /// DateTime the job was submitted at (time the ExtractorCL was run)
        /// </summary>
        public DateTime JobSubmittedAt { get; }

        /// <summary>
        /// Reference number for the project
        /// </summary>
        [NotNull]
        public string ProjectNumber { get; }

        /// <summary>
        /// Working directory for this project
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

        public ExtractJobInfo(
            Guid extractionJobIdentifier,
            DateTime jobSubmittedAt,
            [NotNull] string projectNumber,
            [NotNull] string extractionDirectory,
            [NotNull] string keyTag,
            uint keyValueCount,
            [CanBeNull] string extractionModality,
            ExtractJobStatus jobStatus)
        {
            ExtractionJobIdentifier = (extractionJobIdentifier != default) ? extractionJobIdentifier : throw new ArgumentNullException(nameof(extractionJobIdentifier));
            JobSubmittedAt = (jobSubmittedAt != default) ? jobSubmittedAt : throw new ArgumentNullException(nameof(jobSubmittedAt));
            ProjectNumber = (!string.IsNullOrWhiteSpace(projectNumber)) ? projectNumber : throw new ArgumentNullException(nameof(projectNumber));
            ExtractionDirectory = (!string.IsNullOrWhiteSpace(extractionDirectory)) ? extractionDirectory : throw new ArgumentNullException(nameof(extractionDirectory));
            KeyTag = (!string.IsNullOrWhiteSpace(keyTag)) ? keyTag : throw new ArgumentNullException(nameof(keyTag));
            KeyValueCount = (keyValueCount > 0) ? keyValueCount : throw new ArgumentNullException(nameof(keyValueCount));
            if (extractionModality != null)
                ExtractionModality = (!string.IsNullOrWhiteSpace(extractionModality)) ? extractionModality : throw new ArgumentNullException(nameof(extractionModality));
            JobStatus = (jobStatus != default) ? jobStatus : throw new ArgumentException(nameof(jobStatus));
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
                hashCode = (hashCode * 397) ^ (int)KeyValueCount;
                hashCode = (hashCode * 397) ^ (ExtractionModality != null ? ExtractionModality.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)JobStatus;
                return hashCode;
            }
        }

        #endregion
    }

    // TODO(rkm 2020-03-09) Now only needed for reporting - check if we still need all the properties
    /// <summary>
    /// Container class to store information from a single <see cref="ExtractFileCollectionInfoMessage"/>
    /// </summary>
    public class ExtractFileCollectionInfo
    {
        /// <summary>
        /// Value matched to each of the files sent for anonymisation
        /// </summary>
        [NotNull]
        public string KeyValue { get; }

        /// <summary>
        /// List of all the expected anonymised file paths, relative to the ExtractionDirectory
        /// </summary>
        [NotNull]
        public List<string> ExpectedAnonymisedFiles { get; }

        /// <summary>
        /// Information on all the matched files which were rejected before anonymisation. Can be empty
        /// </summary>
        [NotNull]
        public Dictionary<string, int> RejectionInfo { get; }

        public ExtractFileCollectionInfo(
            [NotNull] string keyValue,
            [NotNull] List<string> expectedAnonymisedFiles,
            [NotNull] Dictionary<string, int> rejectionInfo)
        {
            KeyValue = (!string.IsNullOrWhiteSpace(keyValue)) ? keyValue : throw new ArgumentNullException(nameof(keyValue));
            ExpectedAnonymisedFiles = (expectedAnonymisedFiles.Count > 0) ? expectedAnonymisedFiles : throw new ArgumentNullException(nameof(expectedAnonymisedFiles));
            RejectionInfo = rejectionInfo ?? throw new ArgumentNullException(nameof(rejectionInfo));
        }

        #region Equality Members

        private bool Equals(ExtractFileCollectionInfo other)
        {
            return KeyValue == other.KeyValue &&
                   ExpectedAnonymisedFiles.OrderBy(x => x).SequenceEqual(other.ExpectedAnonymisedFiles.OrderBy(x => x)) &&
                   RejectionInfo.OrderBy(x => x.Key).SequenceEqual(other.RejectionInfo.OrderBy(x => x.Key));
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ExtractFileCollectionInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = KeyValue.GetHashCode();
                hashCode = (hashCode * 397) ^ ExpectedAnonymisedFiles.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y.GetHashCode());
                hashCode = (hashCode * 397) ^ RejectionInfo.Distinct().Aggregate(0, (x, y) => x.GetHashCode() ^ y.GetHashCode());
                return hashCode;
            }
        }

        #endregion
    }

    // TODO(rkm 2020-03-09) Now only needed for reporting - check if we still need all the properties
    /// <summary>
    /// Container class to store information from a single <see cref="ExtractFileStatusMessage"/>
    /// </summary>
    public sealed class ExtractFileStatusInfo : IEquatable<ExtractFileStatusInfo>
    {
        /// <summary>
        /// Filename for the anonymised file, if it exists, otherwise null
        /// </summary>
        [CanBeNull]
        public string AnonymisedFileName { get; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsIdentifiable { get; }

        /// <summary>
        /// Will be the failure reason from an ExtractFileStatusMessage, or the report content from an IsIdentifiableMessage
        /// </summary>
        [NotNull]
        public string StatusMessage { get; }

        public ExtractFileStatusInfo(
            [CanBeNull] string anonymisedFileName,
            bool isIdentifiable,
            [NotNull] string statusMessage)
        {
            AnonymisedFileName = anonymisedFileName;
            IsIdentifiable = isIdentifiable;
            StatusMessage = statusMessage ?? throw new ArgumentNullException(nameof(statusMessage));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("AnonymisedFileName: " + AnonymisedFileName);
            sb.AppendLine("IsIdentifiable: " + IsIdentifiable);
            sb.AppendLine("StatusMessage: " + StatusMessage);
            return sb.ToString();
        }

        #region Equality Members

        public bool Equals(ExtractFileStatusInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(AnonymisedFileName, other.AnonymisedFileName) &&
                   IsIdentifiable == other.IsIdentifiable &&
                   string.Equals(StatusMessage, other.StatusMessage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ExtractFileStatusInfo && Equals((ExtractFileStatusInfo)obj);
        }

        public static bool operator ==(ExtractFileStatusInfo left, ExtractFileStatusInfo right) => Equals(left, right);

        public static bool operator !=(ExtractFileStatusInfo left, ExtractFileStatusInfo right) => !Equals(left, right);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (AnonymisedFileName != null ? AnonymisedFileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsIdentifiable.GetHashCode();
                hashCode = (hashCode * 397) ^ (StatusMessage != null ? StatusMessage.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
