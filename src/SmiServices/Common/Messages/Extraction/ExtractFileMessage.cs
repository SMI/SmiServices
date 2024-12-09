
using Newtonsoft.Json;

namespace SmiServices.Common.Messages.Extraction
{
    /// <summary>
    /// Describes a single image which should be extracted and anonymised using the provided anonymisation script
    /// </summary>
    public class ExtractFileMessage : ExtractMessage, IFileReferenceMessage
    {
        /// <summary>
        /// The file path where the original dicom file can be found, relative to the FileSystemRoot
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomFilePath { get; set; } = null!;

        /// <summary>
        /// The subdirectory and dicom filename within the ExtractionDirectory to extract the identifiable image (specified by <see cref="DicomFilePath"/>) into.  For example
        /// "Series132\1234-an.dcm"
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string OutputPath { get; set; } = null!;


        [JsonConstructor]
        public ExtractFileMessage() { }

        public ExtractFileMessage(ExtractionRequestMessage request)
            : base(request) { }
    }
}
