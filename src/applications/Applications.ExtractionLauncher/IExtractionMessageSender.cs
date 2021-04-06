using System.Collections.Generic;
using Smi.Common.Messages.Extraction;


namespace Applications.ExtractionLauncher
{
    public interface IExtractionMessageSender
    {
        void SendMessages(ExtractionKey extractionKey, List<string> idList);
    }
}