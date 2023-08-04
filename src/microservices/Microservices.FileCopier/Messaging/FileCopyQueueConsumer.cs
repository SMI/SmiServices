using Microservices.FileCopier.Execution;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using System;

namespace Microservices.FileCopier.Messaging
{
    public class FileCopyQueueConsumer : Consumer<ExtractFileMessage>
    {
        private readonly IFileCopier _fileCopier;

        public FileCopyQueueConsumer(
            IFileCopier fileCopier)
        {
            _fileCopier = fileCopier;
        }

        protected override void ProcessMessageImpl(
            IMessageHeader header,
            ExtractFileMessage message,
            ulong tag)
        {
            if (!message.IsIdentifiableExtraction)
                throw new ArgumentException("Received a message with IsIdentifiableExtraction not set");

            try
            {
                _fileCopier.ProcessMessage(message, header);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage
                ErrorAndNack(header, tag, "Error while processing ExtractedFileStatusMessage", e);
                return;
            }

            Ack(header, tag);
        }
    }
}
