using System.Collections.Generic;
using SmiServices.Common.Messages.Extraction;


namespace SmiServices.Applications.ExtractImages
{
    public interface IExtractionMessageSender
    {
        void SendMessages(ExtractionKey extractionKey, List<string> idList);
    }
}
