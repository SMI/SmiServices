
using System;
using Newtonsoft.Json;

namespace Smi.Common.Messages.Extraction
{
    /// <summary>
    /// Status message sent by services which extract files (CTP, FileCopier)
    /// </summary>
    public class ExtractedFileStatusMessage : ExtractMessage, IFileReferenceMessage, IEquatable<ExtractedFileStatusMessage>
    {
        /// <summary>
        /// Original file path
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomFilePath { get; set; }

        /// <summary>
        /// The <see cref="ExtractedFileStatus"/> for this file
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ExtractedFileStatus Status { get; set; }

        /// <summary>
        /// Output file path, relative to the extraction directory. Only required if an output file has been produced
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public string OutputFilePath { get; set; }

        /// <summary>
        /// Message required if Status is not 0
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public string StatusMessage { get; set; }


        [JsonConstructor]
        public ExtractedFileStatusMessage() { }

        public ExtractedFileStatusMessage(IExtractMessage request)
            : base(request) { }

        public override string ToString() =>
            $"{base.ToString()}," +
            $"DicomFilePath={DicomFilePath}," +
            $"ExtractedFileStatus={Status}," +
            $"OutputFilePath={OutputFilePath}," +
            $"StatusMessage={StatusMessage}," +
            "";

        #region Equality Members

        public bool Equals(ExtractedFileStatusMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) &&
                   Status == other.Status &&
                   string.Equals(OutputFilePath, other.OutputFilePath) &&
                   string.Equals(StatusMessage, other.StatusMessage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractedFileStatusMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (DicomFilePath != null ? DicomFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ (OutputFilePath != null ? OutputFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StatusMessage != null ? StatusMessage.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ExtractedFileStatusMessage left, ExtractedFileStatusMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtractedFileStatusMessage left, ExtractedFileStatusMessage right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
