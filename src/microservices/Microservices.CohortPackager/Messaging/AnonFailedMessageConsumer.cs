using System;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;


namespace Microservices.CohortPackager.Messaging
{
    /// <summary>
    /// Consumer for <see cref="ExtractFileStatusMessage"/>(s)
    /// </summary>
    public class AnonFailedMessageConsumer : Consumer
    {
        private readonly IExtractJobStore _store;


        public AnonFailedMessageConsumer(IExtractJobStore store)
        {
            _store = store;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs ea)
        {
            if (!SafeDeserializeToMessage(header, ea, out ExtractFileStatusMessage message))
                return;

            try
            {
                _store.PersistMessageToStore(message, header);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage
                ErrorAndNack(header, ea, "Error while processing ExtractFileStatusMessage", e);
                return;
            }

            Ack(header, ea);
        }
    }
}
