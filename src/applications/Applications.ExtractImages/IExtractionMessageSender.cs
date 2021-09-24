using System.Collections.Generic;
using Smi.Common.Messages.Extraction;


namespace Applications.ExtractImages
{
    public interface IExtractionMessageSender
    {
        void SendMessages(string absoluteExtractionDir, ExtractionKey extractionKey, List<string> idList);
    }
}