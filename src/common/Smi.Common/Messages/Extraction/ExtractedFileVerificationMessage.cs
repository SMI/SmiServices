using Newtonsoft.Json;

namespace Smi.Common.Messages.Extraction
{
    public class ExtractedFileVerificationMessage : ExtractMessage, IFileReferenceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public VerifiedFileStatus Status { get; set; }

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
        public ExtractedFileVerificationMessage() { }

        public ExtractedFileVerificationMessage(ExtractedFileStatusMessage request)
            : base(request)
        {
            DicomFilePath = request.DicomFilePath;
            OutputFilePath = request.OutputFilePath;
        }
    }
}
