
using JetBrains.Annotations;
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

        [JsonProperty(Required = Required.Always)]
        public bool IsIdentifiableExtraction { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool IsNoFilterExtraction { get; set; }

        [JsonConstructor]
        protected ExtractMessage() { }


        protected ExtractMessage(
            Guid extractionJobIdentifier,
            [NotNull] string projectNumber,
            [NotNull] string extractionDirectory,
            DateTime jobSubmittedAt,
            bool isIdentifiableExtraction,
            bool isNoFilterExtraction)
            : this()
        {
            ExtractionJobIdentifier = extractionJobIdentifier;
            ProjectNumber = projectNumber;
            ExtractionDirectory = extractionDirectory;
            JobSubmittedAt = jobSubmittedAt;
            IsIdentifiableExtraction = isIdentifiableExtraction;
            IsNoFilterExtraction = isNoFilterExtraction;
        }

        protected ExtractMessage(IExtractMessage request)
            : this(
                request.ExtractionJobIdentifier,
                request.ProjectNumber,
                request.ExtractionDirectory,
                request.JobSubmittedAt,
                request.IsIdentifiableExtraction,
                request.IsNoFilterExtraction)
        { }

        public override string ToString() =>
            $"ExtractionJobIdentifier={ExtractionJobIdentifier}, " +
            $"ProjectNumber={ProjectNumber}, " +
            $"ExtractionDirectory={ExtractionDirectory}, " +
            $"JobSubmittedAt={JobSubmittedAt:s}, " +
            $"IsIdentifiableExtraction={IsIdentifiableExtraction}, " +
            $"IsNoFilterExtraction={IsNoFilterExtraction}, " +
            "";

        #region Equality Members

        public bool Equals(ExtractMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return ExtractionJobIdentifier.Equals(other.ExtractionJobIdentifier)
                   && string.Equals(ProjectNumber, other.ProjectNumber)
                   && string.Equals(ExtractionDirectory, other.ExtractionDirectory)
                   && JobSubmittedAt.Equals(other.JobSubmittedAt)
                   && IsIdentifiableExtraction == other.IsIdentifiableExtraction
                   && IsNoFilterExtraction == other.IsNoFilterExtraction;
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
                hashCode = (hashCode * 397) ^ ProjectNumber.GetHashCode();
                hashCode = (hashCode * 397) ^ ExtractionDirectory.GetHashCode();
                hashCode = (hashCode * 397) ^ JobSubmittedAt.GetHashCode();
                hashCode = (hashCode * 397) ^ IsIdentifiableExtraction.GetHashCode();
                hashCode = (hashCode * 397) ^ IsNoFilterExtraction.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ExtractMessage left, ExtractMessage right) => Equals(left, right);

        public static bool operator !=(ExtractMessage left, ExtractMessage right) => !Equals(left, right);

        #endregion
    }
}
