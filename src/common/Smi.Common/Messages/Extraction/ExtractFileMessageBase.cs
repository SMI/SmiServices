using Newtonsoft.Json;

namespace Smi.Common.Messages.Extraction
{
    /// <summary>
    /// Base class for all messages which describe extraction of a single file
    /// </summary>
    public abstract class ExtractFileMessageBase : ExtractMessage, IExtractFileMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string DicomFilePath { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string OutputPath { get; set; }


        [JsonProperty(Required = Required.Always)]
        public string StudyInstanceUID { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string SeriesInstanceUID { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string SOPInstanceUID { get; set; }


        [JsonProperty(Required = Required.AllowNull)]
        public string ReplacementStudyInstanceUID { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string ReplacementSeriesInstanceUID { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string ReplacementSOPInstanceUID { get; set; }


        [JsonConstructor]
        public ExtractFileMessageBase() { }

        protected ExtractFileMessageBase(ExtractFileMessageBase request)
            : base(request) { }
    }
}
