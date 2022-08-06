namespace Smi.Common.Messages.Extraction
{
    public class ExtractionRequestInfoMessage : ExtractMessage
    {
        public string KeyTag { get; set; }

        public int KeyValueCount { get; set; }

        public string ExtractionModality { get; set; }


        public ExtractionRequestInfoMessage() { }

        public override string ToString()
        {
            return base.ToString() + $",KeyTag={KeyTag},KeyValueCount={KeyValueCount},ExtractionModality={ExtractionModality}";
        }
    }
}
