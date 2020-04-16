
using Newtonsoft.Json;
using System;

namespace Smi.Common.Messages.Extraction
{
    /// <summary>
    /// Base class for all messages relating to the extract process
    /// </summary>
    public abstract class ExtractMessage : IExtractMessage, IEquatable<ExtractMessage>
    {
        [JsonProperty(Required = Required.Always)]
        public Guid ExtractionJobIdentifier { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ProjectNumber { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ExtractionDirectory { get; set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime JobSubmittedAt { get; set; }


        [JsonConstructor]
        protected ExtractMessage() { }

        protected ExtractMessage(Guid extractionJobIdentifier, string projectNumber, string extractionDirectory, DateTime jobSubmittedAt)
            : this()
        {
            ExtractionJobIdentifier = extractionJobIdentifier;
            ProjectNumber = projectNumber;
            ExtractionDirectory = extractionDirectory;
            JobSubmittedAt = jobSubmittedAt;
        }

        protected ExtractMessage(IExtractMessage request)
            : this(request.ExtractionJobIdentifier, request.ProjectNumber, request.ExtractionDirectory, request.JobSubmittedAt)
        { }

        public override string ToString()
            => $"ExtractionJobIdentifier={ExtractionJobIdentifier}, " +
               $"ProjectNumber={ProjectNumber}, " +
               $"ExtractionDirectory={ExtractionDirectory}, " +
               $"JobSubmittedAt={JobSubmittedAt}";

        #region Equality Members

        public bool Equals(ExtractMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return ExtractionJobIdentifier.Equals(other.ExtractionJobIdentifier) &&
                   string.Equals(ProjectNumber, other.ProjectNumber) &&
                   string.Equals(ExtractionDirectory, other.ExtractionDirectory) &&
                   JobSubmittedAt.Equals(other.JobSubmittedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ExtractionJobIdentifier.GetHashCode();
                hashCode = (hashCode * 397) ^ (ProjectNumber != null ? ProjectNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ExtractionDirectory != null ? ExtractionDirectory.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ JobSubmittedAt.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ExtractMessage left, ExtractMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtractMessage left, ExtractMessage right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
