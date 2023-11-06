using Equ;
using Newtonsoft.Json;
using System;

namespace Smi.Common.Messages.Extraction
{
    /// <summary>
    /// Base class for all messages relating to the extract process
    /// </summary>
    public abstract class ExtractMessage : MemberwiseEquatable<ExtractMessage>, IExtractMessage
    {
        [JsonProperty(Required = Required.Always)]
        public Guid ExtractionJobIdentifier { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ProjectNumber { get; set; } = null!;

        [JsonProperty(Required = Required.Always)]
        public string ExtractionDirectory { get; set; } = null!;

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
            string projectNumber,
            string extractionDirectory,
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
    }
}
