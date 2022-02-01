using Newtonsoft.Json;

namespace Smi.Common.Messages.Extraction
{
    public class ExtractedFileVerificationMessage : ExtractFileMessageBase
    {
        [JsonProperty(Required = Required.Always)]
        public bool IsIdentifiable { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Report { get; set; }

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
