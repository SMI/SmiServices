using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;


namespace Microservices.FileCopier.Execution
{
    public interface IFileCopier
    {
        void ProcessMessage(ExtractFileMessage message, IMessageHeader header);
    }
}
