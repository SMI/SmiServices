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
        protected ExtractFileMessageBase() { }

        protected ExtractFileMessageBase(IExtractFileMessage request)
            : base(request)
        {
            DicomFilePath = request.DicomFilePath;
            OutputPath = request.OutputPath;
            StudyInstanceUID = request.StudyInstanceUID;
            SeriesInstanceUID = request.SeriesInstanceUID;
            SOPInstanceUID = request.SOPInstanceUID;
            ReplacementStudyInstanceUID = request.ReplacementStudyInstanceUID;
            ReplacementSeriesInstanceUID = request.ReplacementSeriesInstanceUID;
            ReplacementSOPInstanceUID = request.ReplacementSOPInstanceUID;
        }

        public override string ToString() =>
            base.ToString() +
            $"DicomFilePath={DicomFilePath}, " +
            $"OutputPath={OutputPath}, " +
            $"StudyInstanceUID={StudyInstanceUID}, " +
            $"SeriesInstanceUID={SeriesInstanceUID}, " +
            $"SOPInstanceUID={SOPInstanceUID}, " +
            $"ReplacementStudyInstanceUID={ReplacementStudyInstanceUID}, " +
            $"ReplacementSeriesInstanceUID={ReplacementSeriesInstanceUID}, " +
            $"ReplacementSOPInstanceUID={ReplacementSOPInstanceUID}, " +
            "";
    }
}
