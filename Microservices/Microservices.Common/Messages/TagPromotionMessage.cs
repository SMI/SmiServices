
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microservices.Common.Messages
{
    public sealed class TagPromotionMessage : IMessage
    {
        /// <summary>
        /// Dicom tag (0020,000D)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string StudyInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0020,000E)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SeriesInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0008,0018)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SOPInstanceUID { get; set; }

        /// <summary>
        /// The tags to promote. Key is the dictionary entry for the DicomTag
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, object> PromotedTags { get; set; }

        #region Equality Members

        private bool Equals(TagPromotionMessage other)
        {
            return string.Equals(StudyInstanceUID, other.StudyInstanceUID) && 
                   string.Equals(SeriesInstanceUID, other.SeriesInstanceUID) && 
                   string.Equals(SOPInstanceUID, other.SOPInstanceUID) && 
                   Equals(PromotedTags, other.PromotedTags);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TagPromotionMessage && Equals((TagPromotionMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (StudyInstanceUID != null ? StudyInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SeriesInstanceUID != null ? SeriesInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SOPInstanceUID != null ? SOPInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PromotedTags != null ? PromotedTags.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
