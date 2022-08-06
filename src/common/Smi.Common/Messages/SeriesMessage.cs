using Equ;

namespace Smi.Common.Messages
{
    /// <inheritdoc />
    /// <summary>
    /// Object representing a series message.
    /// https://github.com/HicServices/SMIPlugin/wiki/SMI-RabbitMQ-messages-and-queues#seriesmessage
    /// </summary>
    public sealed class SeriesMessage : MemberwiseEquatable<SeriesMessage>, IMessage
    {
        /// <summary>
        /// Directory path relative to the root path.
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// Dicom tag (0020,000D).
        /// </summary>
        public string StudyInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0020,000E).
        /// </summary>
        public string SeriesInstanceUID { get; set; }

        /// <summary>
        /// Number of images found in the series.
        /// </summary>
        public int ImagesInSeries { get; set; }

        /// <summary>
        /// Key-value pairs of Dicom tags and their values.
        /// </summary>
        public string DicomDataset { get; set; }
    }
}
