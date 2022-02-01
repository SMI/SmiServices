using System.Text.Json.Serialization;

namespace Smi.Common.Messages.Extraction
{
    public class ExtractFileMessage : ExtractFileMessageBase
    {
        [JsonConstructor]
        public ExtractFileMessage() { }
    }
}
