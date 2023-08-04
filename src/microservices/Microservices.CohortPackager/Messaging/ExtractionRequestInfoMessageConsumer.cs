
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using System;

namespace Microservices.CohortPackager.Messaging

{    /// <summary>
     /// Consumer for <see cref="ExtractionRequestInfoMessage"/>(s)
     /// </summary>
    public class ExtractionRequestInfoMessageConsumer : Consumer<ExtractionRequestInfoMessage>
    {
        private readonly IExtractJobStore _store;


        public ExtractionRequestInfoMessageConsumer(IExtractJobStore store)
        {
            _store = store;
        }

        protected override void ProcessMessageImpl(IMessageHeader? header, ExtractionRequestInfoMessage message, ulong tag)
        {
            try
            {
                _store.PersistMessageToStore(message, header);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage
                ErrorAndNack(header, tag, "Error while processing ExtractionRequestInfoMessage", e);
                return;
            }

            Ack(header, tag);
        }
    }
}
