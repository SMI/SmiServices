using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;


namespace SmiServices.Microservices.FileCopier
{
    public interface IFileCopier
    {
        void ProcessMessage(ExtractFileMessage message, IMessageHeader header);
    }
}
