
using Smi.Common.Messages.Extraction;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public readonly Guid ExtractionJobIdentifier;

        /// <summary>
        /// DateTime the job was submitted at (time the ExtractorCL was run)
        /// </summary>
        public readonly DateTime JobSubmittedAt;

        /// <summary>
        /// Reference number for the project
        /// </summary>
        public readonly string ProjectNumber;

        /// <summary>
        /// Working directory for this project
        /// </summary>
        public readonly string ExtractionDirectory;

        /// <summary>
        /// The DICOM tag of the identifier we are extracting (i.e. "SeriesInstanceUID")
        /// </summary>
        public readonly string KeyTag;

        /// <summary>
        /// Total number of expected distinct identifiers (i.e. number of distinct SeriesInstanceUIDs that are expected to be extracted)
        /// </summary>
        public readonly int KeyValueCount;

        public readonly string ExtractionModality;

        /// <summary>
        /// Current status of the extract job
        /// </summary>
        public readonly ExtractJobStatus JobStatus;

        /// <summary>
        /// Data from the <see cref="ExtractFileCollectionInfoMessage"/>(s) for this job
        /// </summary>
        public readonly List<ExtractFileCollectionInfo> JobFileCollectionInfo;

        //TODO Move this down a level
        /// <summary>
        /// Data from the <see cref="ExtractFileStatusMessage"/>(s) for this job
        /// </summary>
        public readonly List<ExtractFileStatusInfo> JobExtractFileStatuses;


        public ExtractJobInfo(Guid extractionJobIdentifier, string projectNumber, DateTime jobSubmittedAt, ExtractJobStatus jobStatus, string extractionDirectory, int keyValueCount, string keyTag, List<ExtractFileCollectionInfo> jobFileCollectionInfo, List<ExtractFileStatusInfo> jobExtractFileStatuses, string extractionModality)
        {
            ExtractionJobIdentifier = extractionJobIdentifier;
            ProjectNumber = projectNumber;
            JobSubmittedAt = jobSubmittedAt;
            JobStatus = jobStatus;
            ExtractionDirectory = extractionDirectory;
            KeyValueCount = keyValueCount;
            KeyTag = keyTag;
            JobFileCollectionInfo = jobFileCollectionInfo;
            JobExtractFileStatuses = jobExtractFileStatuses;
            ExtractionModality = extractionModality;

            if (!Validate())
                throw new ArgumentException($"The given parameters were not valid (values were {this})");
        }


        private bool Validate()
        {
            var isValid = true;
            isValid &= ExtractionJobIdentifier != Guid.Empty;
            isValid &= !string.IsNullOrWhiteSpace(ProjectNumber);
            isValid &= JobSubmittedAt != default(DateTime);
            isValid &= JobStatus != ExtractJobStatus.Unknown;
            isValid &= !string.IsNullOrWhiteSpace(ExtractionDirectory);
            isValid &= KeyValueCount > 0;
            isValid &= !string.IsNullOrWhiteSpace(KeyTag);
            isValid &= (JobFileCollectionInfo != null && JobFileCollectionInfo.Count == KeyValueCount);
            isValid &= JobExtractFileStatuses != null;
            return isValid;
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
            sb.AppendLine("JobFileCollectionInfo:\n" + string.Join(", ", JobFileCollectionInfo));
            sb.AppendLine("JobExtractFileStatuses:\n" + string.Join(", ", JobExtractFileStatuses));

            return sb.ToString();
        }

        #region Equality Members

        public bool Equals(ExtractJobInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            var isValid = true;
            isValid &= ExtractionJobIdentifier.Equals(other.ExtractionJobIdentifier);
            isValid &= JobStatus == other.JobStatus;
            isValid &= string.Equals(ExtractionDirectory, other.ExtractionDirectory);
            isValid &= KeyValueCount == other.KeyValueCount;
            isValid &= string.Equals(KeyTag, other.KeyTag);
            isValid &= JobFileCollectionInfo.Count == other.JobFileCollectionInfo.Count;
            isValid &= JobFileCollectionInfo.All(other.JobFileCollectionInfo.Contains);
            isValid &= JobExtractFileStatuses.Count == other.JobExtractFileStatuses.Count;
            isValid &= JobExtractFileStatuses.All(other.JobExtractFileStatuses.Contains);
            isValid &= ExtractionModality == other.ExtractionModality;
            return isValid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ExtractJobInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ExtractionJobIdentifier.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)JobStatus;
                hashCode = (hashCode * 397) ^ (ExtractionDirectory != null ? ExtractionDirectory.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ KeyValueCount;
                hashCode = (hashCode * 397) ^ (KeyTag != null ? KeyTag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (JobFileCollectionInfo != null ? JobFileCollectionInfo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (JobExtractFileStatuses != null ? JobExtractFileStatuses.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ExtractionModality != null ? ExtractionModality.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }

    /// <summary>
    /// Container class to store information from a single <see cref="ExtractFileCollectionInfoMessage"/>
    /// </summary>
    public sealed class ExtractFileCollectionInfo : IEquatable<ExtractFileCollectionInfo>
    {
        /// <summary>
        /// Value matched to each of the files sent for anonymisation
        /// </summary>
        public readonly string KeyValue;

        /// <summary>
        /// List of all the expected anonymised file paths, relative to the ExtractionDirectory
        /// </summary>
        public readonly List<string> ExpectedAnonymisedFiles;


        public ExtractFileCollectionInfo(string keyValue, List<string> expectedAnonymisedFiles)
        {
            KeyValue = keyValue;
            ExpectedAnonymisedFiles = expectedAnonymisedFiles;

            if (!Validate())
                throw new ArgumentException("The given parameters were not valid (values were " + ToString() + ")");
        }


        private bool Validate()
        {
            return !string.IsNullOrWhiteSpace(KeyValue) &&
                   ExpectedAnonymisedFiles != null &&
                   ExpectedAnonymisedFiles.Count > 0;
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Key: " + KeyValue);
            sb.AppendLine("ExpectedAnonymisedFiles:\n" + string.Join(", ", ExpectedAnonymisedFiles));

            return sb.ToString();
        }

        #region Equality Members

        public bool Equals(ExtractFileCollectionInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(KeyValue, other.KeyValue) &&
                   ExpectedAnonymisedFiles.All(other.ExpectedAnonymisedFiles.Contains);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ExtractFileCollectionInfo && Equals((ExtractFileCollectionInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((KeyValue != null ? KeyValue.GetHashCode() : 0) * 397) ^ (ExpectedAnonymisedFiles != null ? ExpectedAnonymisedFiles.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ExtractFileCollectionInfo left, ExtractFileCollectionInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtractFileCollectionInfo left, ExtractFileCollectionInfo right)
        {
            return !Equals(left, right);
        }

        #endregion
    }

    /// <summary>
    /// Container class to store information from a single <see cref="ExtractFileStatusMessage"/>
    /// </summary>
    public sealed class ExtractFileStatusInfo : IEquatable<ExtractFileStatusInfo>
    {
        /// <summary>
        /// Status for this anonymised file
        /// </summary>
        public readonly ExtractFileStatus Status;

        /// <summary>
        /// Filename for the anonymised file, if it exists, otherwise null
        /// </summary>
        public readonly string AnonymisedFileName;

        /// <summary>
        /// Optional message if the file could not be anonymised
        /// </summary>
        public readonly string StatusMessage;


        public ExtractFileStatusInfo(string status, string anonymisedFileName, string statusMessage)
            : this(status)
        {
            AnonymisedFileName = anonymisedFileName;
            StatusMessage = statusMessage;

            if (!Validate())
                throw new ArgumentException("The given parameters were not valid (values were " + ToString() + ")");
        }

        private ExtractFileStatusInfo(string stringStatus)
        {
            ExtractFileStatus parsed;

            Status = Enum.TryParse(stringStatus, out parsed)
                ? parsed
                : ExtractFileStatus.Unknown;
        }

        private bool Validate()
        {
            return Status != ExtractFileStatus.Unknown &&
                   ValidateOptionals();
        }

        private bool ValidateOptionals()
        {
            return Status == ExtractFileStatus.Anonymised ?
                !string.IsNullOrWhiteSpace(AnonymisedFileName) :
                !string.IsNullOrWhiteSpace(StatusMessage);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Status: " + Status);
            sb.AppendLine("AnonymisedFileName: " + AnonymisedFileName);
            sb.AppendLine("StatusMessage: " + StatusMessage);
            return sb.ToString();
        }

        #region Equality Members

        public bool Equals(ExtractFileStatusInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Status == other.Status &&
                   string.Equals(AnonymisedFileName, other.AnonymisedFileName) &&
                   string.Equals(StatusMessage, other.StatusMessage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ExtractFileStatusInfo && Equals((ExtractFileStatusInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Status;
                hashCode = (hashCode * 397) ^ (AnonymisedFileName != null ? AnonymisedFileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StatusMessage != null ? StatusMessage.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ExtractFileStatusInfo left, ExtractFileStatusInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtractFileStatusInfo left, ExtractFileStatusInfo right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
