using Equ;
using System.Collections.Generic;

namespace Smi.Common.Messages
{
    public sealed class TagPromotionMessage : MemberwiseEquatable<TagPromotionMessage>, IMessage
    {
        /// <summary>
        /// Dicom tag (0020,000D)
        /// </summary>
        public string StudyInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0020,000E)
        /// </summary>
        public string SeriesInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0008,0018)
        /// </summary>
        public string SOPInstanceUID { get; set; }

        /// <summary>
        /// The tags to promote. Key is the dictionary entry for the DicomTag
        /// </summary>
        public Dictionary<string, object> PromotedTags { get; set; }
    }
}
