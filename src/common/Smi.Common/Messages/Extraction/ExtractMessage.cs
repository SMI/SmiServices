using Equ;
using JetBrains.Annotations;
using System;


namespace Smi.Common.Messages.Extraction
{
    /// <summary>
    /// Base class for all messages relating to the extract process
    /// </summary>
    public abstract class ExtractMessage : MemberwiseEquatable<ExtractMessage>, IExtractMessage
    {
        public Guid ExtractionJobIdentifier { get; set; }

        public string ProjectNumber { get; set; }

        public string ExtractionDirectory { get; set; }

        public DateTime JobSubmittedAt { get; set; }

        public bool IsIdentifiableExtraction { get; set; }

        public bool IsNoFilterExtraction { get; set; }

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
    }
}
