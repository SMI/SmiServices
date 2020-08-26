using JetBrains.Annotations;
using Microservices.FileCopier.Execution;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using System;

namespace Microservices.FileCopier.Messaging
{
    public class FileCopyQueueConsumer : Consumer<ExtractFileMessage>
    {
        [NotNull] private readonly IFileCopier _fileCopier;

        public FileCopyQueueConsumer(
            [NotNull] IFileCopier fileCopier)
        {
            _fileCopier = fileCopier;
        }

        protected override void ProcessMessageImpl(
            [NotNull] IMessageHeader header,
            [NotNull] ExtractFileMessage message,
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
                ErrorAndNack(header, tag, "Error while processing ExtractFileStatusMessage", e);
                return;
            }

            Ack(header, tag);
        }
    }
}
