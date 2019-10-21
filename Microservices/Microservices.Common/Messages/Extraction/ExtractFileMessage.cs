
using Newtonsoft.Json;
using System;

namespace Microservices.Common.Messages.Extraction
{
    /// <summary>
    /// Describes a single image which should be extracted and anonymised using the provided anonymisation script
    /// </summary>
    public class ExtractFileMessage : ExtractMessage, IFileReferenceMessage, IEquatable<ExtractFileMessage>
    {
        /// <summary>
        /// The file path where the original dicom file can be found, relative to the FileSystemRoot
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomFilePath { get; set; }

        /// <summary>
        /// The subdirectory and dicom filename within the ExtractionDirectory to extract the identifiable image (specified by <see cref="DicomFilePath"/>) into.  For example
        /// "Series132\1234-an.dcm"
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string OutputPath { get; set; }


        [JsonConstructor]
        private ExtractFileMessage() { }

        public ExtractFileMessage(ExtractionRequestMessage request)
            : base(request) { }


        #region Equality Members

        public bool Equals(ExtractFileMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) &&
                   string.Equals(DicomFilePath, other.DicomFilePath) &&
                   string.Equals(OutputPath, other.OutputPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractFileMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (DicomFilePath != null ? DicomFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OutputPath != null ? OutputPath.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
