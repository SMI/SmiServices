using System;
using Newtonsoft.Json;

namespace Smi.Common.Messages.Extraction
{
    // TODO(rkm 2020-02-04) Rename to AnonVerificationMessage
    public class IsIdentifiableMessage : ExtractMessage, IFileReferenceMessage, IEquatable<IsIdentifiableMessage>
    {
        [JsonProperty(Required = Required.Always)]
        public bool IsIdentifiable { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Report { get; set; }

        /// <summary>
        /// The originally sourced origin (identifiable file path).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomFilePath { get; set; }

        /// <summary>
        /// Output file path, relative to the extraction directory. Only required if an output file has been produced
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string OutputFilePath { get; set; }

        [JsonConstructor]
        public IsIdentifiableMessage() { }

        public IsIdentifiableMessage(Guid extractionJobIdentifier, string projectNumber, string extractionDirectory, DateTime jobSubmittedAt)
            : this()
        {
            ExtractionJobIdentifier = extractionJobIdentifier;
            ProjectNumber = projectNumber;
            ExtractionDirectory = extractionDirectory;
            JobSubmittedAt = jobSubmittedAt;
        }

        /// <summary>
        /// Creates a new instance copying all values from the given origin message
        /// </summary>
        /// <param name="request"></param>
        public IsIdentifiableMessage(ExtractedFileStatusMessage request)
            : this(request.ExtractionJobIdentifier, request.ProjectNumber, request.ExtractionDirectory,
                request.JobSubmittedAt)
        {
            DicomFilePath = request.DicomFilePath;
            OutputFilePath = request.OutputFilePath;
        }

        #region Equality Members

        public bool Equals(IsIdentifiableMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && IsIdentifiable == other.IsIdentifiable && DicomFilePath == other.DicomFilePath && OutputFilePath == other.OutputFilePath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IsIdentifiableMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ IsIdentifiable.GetHashCode();
                hashCode = (hashCode * 397) ^ (DicomFilePath != null ? DicomFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OutputFilePath != null ? OutputFilePath.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion

    }
}
