using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;


namespace SmiServices.Microservices.FileCopier;

public interface IFileCopier
{
    void ProcessMessage(ExtractFileMessage message, IMessageHeader header);
}
