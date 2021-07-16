
using Newtonsoft.Json;

namespace Smi.Common.Messages
{
    /// <inheritdoc />
    /// <summary>
    /// Object representing a series message.
    /// https://github.com/HicServices/SMIPlugin/wiki/SMI-RabbitMQ-messages-and-queues#seriesmessage
    /// </summary>
    public sealed class SeriesMessage : IMessage
    {
        /// <summary>
        /// Directory path relative to the root path.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DirectoryPath { get; set; }

        /// <summary>
        /// Dicom tag (0020,000D).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string StudyInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0020,000E).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SeriesInstanceUID { get; set; }

        /// <summary>
        /// Number of images found in the series.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int ImagesInSeries { get; set; }

        /// <summary>
        /// Key-value pairs of Dicom tags and their values.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomDataset { get; set; }


        #region Equality Members

        private bool Equals(SeriesMessage other)
        {
            return                
                string.Equals(DirectoryPath, other.DirectoryPath) &&
                string.Equals(StudyInstanceUID, other.StudyInstanceUID) &&
                string.Equals(SeriesInstanceUID, other.SeriesInstanceUID) &&
                ImagesInSeries == other.ImagesInSeries &&
                string.Equals(DicomDataset, other.DicomDataset);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SeriesMessage && Equals((SeriesMessage) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (DirectoryPath != null ? DirectoryPath.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (StudyInstanceUID != null ? StudyInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (SeriesInstanceUID != null ? SeriesInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ ImagesInSeries;
                hashCode = (hashCode*397) ^ (DicomDataset != null ? DicomDataset.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(SeriesMessage left, SeriesMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SeriesMessage left, SeriesMessage right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
