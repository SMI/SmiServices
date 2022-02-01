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

        [JsonConstructor]
        public ExtractFileMessageBase() { }

        protected ExtractFileMessageBase(ExtractFileMessageBase request)
            : base(request) { }
    }
}
