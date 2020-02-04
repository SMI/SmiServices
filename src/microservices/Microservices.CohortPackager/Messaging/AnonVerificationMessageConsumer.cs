
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using RabbitMQ.Client.Events;
using System;

namespace Microservices.CohortPackager.Messaging
{
    /// <summary>
    /// Consumer for <see cref="IsIdentifiableMessage"/>(s)
    /// </summary>
    public class AnonVerificationMessageConsumer : Consumer
    {
        private readonly IExtractJobStore _store;


        public AnonVerificationMessageConsumer(IExtractJobStore store)
        {
            _store = store;
        }


        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs ea)
        {
            if (!SafeDeserializeToMessage(header, ea, out IsIdentifiableMessage message))
                return;

            // TODO(rkm 2020-02-02)
            throw new NotImplementedException();
            //try
            //{
            //    _store.PersistMessageToStore(message, header);
            //}
            //catch (ApplicationException e)
            //{
            //    // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage

            //    ErrorAndNack(header, ea, "Error while processing ExtractFileStatusMessage", e);
            //    return;
            //}
            //Ack(header, ea);
        }
    }
}
