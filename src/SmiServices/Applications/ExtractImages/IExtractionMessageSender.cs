using System.Collections.Generic;
using Smi.Common.Messages.Extraction;


namespace SmiServices.Applications.ExtractImages
{
    public interface IExtractionMessageSender
    {
        void SendMessages(ExtractionKey extractionKey, List<string> idList);
    }
}
