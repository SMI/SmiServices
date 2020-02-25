
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
    public class ExtractFileCollectionMessageConsumer : Consumer
    {
        private readonly IExtractJobStore _store;


        public ExtractFileCollectionMessageConsumer(IExtractJobStore store)
        {
            _store = store;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs ea)
        {
            if (!SafeDeserializeToMessage(header, ea, out ExtractFileCollectionInfoMessage message))
                return;

            try
            {
                _store.PersistMessageToStore(message, header);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage
                ErrorAndNack(header, ea, "Error while processing ExtractFileCollectionInfoMessage", e);
                return;
            }

            Ack(header, ea);
        }
    }
}
