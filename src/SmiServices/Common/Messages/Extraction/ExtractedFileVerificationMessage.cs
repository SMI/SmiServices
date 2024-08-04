using Newtonsoft.Json;
using System;

namespace SmiServices.Common.Messages.Extraction
{
    public class ExtractedFileVerificationMessage : ExtractMessage, IFileReferenceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public VerifiedFileStatus Status { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Report { get; set; } = null!;

        /// <summary>
        /// The originally sourced origin (identifiable file path).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomFilePath { get; set; } = null!;

        /// <summary>
        /// Output file path, relative to the extraction directory. Only required if an output file has been produced
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string OutputFilePath { get; set; } = null!;

        [JsonConstructor]
        public ExtractedFileVerificationMessage() { }

        public ExtractedFileVerificationMessage(ExtractedFileStatusMessage request)
            : base(request)
        {
            DicomFilePath = request.DicomFilePath;
            OutputFilePath = request.OutputFilePath ?? throw new ArgumentNullException(nameof(request.OutputFilePath));
        }
    }
}
