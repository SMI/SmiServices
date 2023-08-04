using System;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;


namespace Microservices.CohortPackager.Messaging
{
    // TODO Naming
    /// <summary>
    /// Consumer for <see cref="ExtractedFileStatusMessage"/>(s)
    /// </summary>
    public class AnonFailedMessageConsumer : Consumer<ExtractedFileStatusMessage>
    {
        private readonly IExtractJobStore _store;


        public AnonFailedMessageConsumer(IExtractJobStore store)
        {
            _store = store;
        }

        protected override void ProcessMessageImpl(IMessageHeader? header, ExtractedFileStatusMessage message, ulong tag)
        {
            try
            {
                _store.PersistMessageToStore(message, header);
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
