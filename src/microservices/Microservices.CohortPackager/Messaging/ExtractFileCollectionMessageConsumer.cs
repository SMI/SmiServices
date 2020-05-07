
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using RabbitMQ.Client.Events;
using System;

namespace Microservices.CohortPackager.Messaging
{
    /// <summary>
    /// Consumer for <see cref="ExtractFileCollectionInfoMessage"/>(s)
    /// </summary>
    public class ExtractFileCollectionMessageConsumer : Consumer<ExtractFileCollectionInfoMessage>
    {
        private readonly IExtractJobStore _store;


        public ExtractFileCollectionMessageConsumer(IExtractJobStore store)
        {
            _store = store;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, ExtractFileCollectionInfoMessage message, ulong tag)
        {
            try
            {
                _store.PersistMessageToStore(message, header);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage
                ErrorAndNack(header, tag, "Error while processing ExtractFileCollectionInfoMessage", e);
                return;
            }

            Ack(header, tag);
        }
    }
}
