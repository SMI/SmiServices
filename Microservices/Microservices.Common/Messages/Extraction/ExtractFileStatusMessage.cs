
using System;
using Newtonsoft.Json;

namespace Microservices.Common.Messages.Extraction
{
    /// <summary>
    /// Status message received from the anonymisation service
    /// </summary>
    public class ExtractFileStatusMessage : ExtractMessage, IFileReferenceMessage, IEquatable<ExtractFileStatusMessage>
    {
        /// <summary>
        /// Original file path
        /// </summary>
        public string DicomFilePath { get; set; }

        /// <summary>
        /// The <see cref="ExtractFileStatus"/> for this file
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ExtractFileStatus Status { get; set; }

        /// <summary>
        /// Anonymised file name. Only required if a file has been anonymised
        /// </summary>
        [JsonProperty(Required = Required.DisallowNull)]
        public string AnonymisedFileName { get; set; }

        /// <summary>
        /// Message required if Status is not 0
        /// </summary>
        [JsonProperty(Required = Required.DisallowNull)]
        public string StatusMessage { get; set; }


        [JsonConstructor]
        public ExtractFileStatusMessage() { }


        #region Equality Members

        public bool Equals(ExtractFileStatusMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) &&
                   Status == other.Status &&
                   string.Equals(AnonymisedFileName, other.AnonymisedFileName) &&
                   string.Equals(StatusMessage, other.StatusMessage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractFileStatusMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (DicomFilePath != null ? DicomFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ (AnonymisedFileName != null ? AnonymisedFileName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StatusMessage != null ? StatusMessage.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ExtractFileStatusMessage left, ExtractFileStatusMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtractFileStatusMessage left, ExtractFileStatusMessage right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
