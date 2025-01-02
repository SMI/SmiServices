using SmiServices.Common.Messages.Extraction;
using System.Collections.Generic;


namespace SmiServices.Applications.ExtractImages;

public interface IExtractionMessageSender
{
    void SendMessages(ExtractionKey extractionKey, List<string> idList);
}
