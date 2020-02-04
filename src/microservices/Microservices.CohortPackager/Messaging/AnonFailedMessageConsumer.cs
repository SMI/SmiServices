
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using RabbitMQ.Client.Events;
using System;
using System.ComponentModel;
using Renci.SshNet.Messages;


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

            if (message.Status == ExtractFileStatus.Anonymised)
                throw new ApplicationException("Received an anonymisation successful message from the failure queue");

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
