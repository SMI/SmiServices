
using Equ;
using Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public string DirectoryPath { get; set; } = null!;

        /// <summary>
        /// Dicom tag (0020,000D).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string StudyInstanceUID { get; set; } = null!;

        /// <summary>
        /// Dicom tag (0020,000E).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SeriesInstanceUID { get; set; } = null!;

        /// <summary>
        /// Number of images found in the series.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int ImagesInSeries { get; set; }

        /// <summary>
        /// Key-value pairs of Dicom tags and their values.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomDataset { get; set; } = null!;
    }
}
