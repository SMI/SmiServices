using System;
using Newtonsoft.Json;

namespace Smi.Common.Messages.Extraction
{
    public class IsIdentifiableMessage : ExtractMessage,IFileReferenceMessage,IMessage
    {
        public bool IsIdentifiable { get; set; }

        /// <summary>
        /// The originally sourced origin (identifiable file path).
        /// </summary>
        public string DicomFilePath { get; set; }

        /// <summary>
        /// Anonymised file name. Only required if a file has been anonymised
        /// </summary>
        [JsonProperty(Required = Required.DisallowNull)]
        public string AnonymisedFileName { get; set; }
        
        [JsonConstructor]
        protected IsIdentifiableMessage() { }

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
        public IsIdentifiableMessage(ExtractFileStatusMessage request)
            : this(request.ExtractionJobIdentifier, request.ProjectNumber, request.ExtractionDirectory,
                request.JobSubmittedAt)
        {
            DicomFilePath = request.DicomFilePath;
            AnonymisedFileName = request.AnonymisedFileName;
        }

    }
}