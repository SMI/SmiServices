
using IsIdentifiable.Reporting;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Newtonsoft.Json;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using System;
using System.Collections.Generic;


namespace Microservices.CohortPackager.Messaging
{
    /// <summary>
    /// Consumer for <see cref="ExtractedFileVerificationMessage"/>(s)
    /// </summary>
    public class AnonVerificationMessageConsumer : Consumer<ExtractedFileVerificationMessage>
    {
        private readonly IExtractJobStore _store;


        public AnonVerificationMessageConsumer(IExtractJobStore store)
        {
            _store = store;
        }


        protected override void ProcessMessageImpl(IMessageHeader? header, ExtractedFileVerificationMessage message, ulong tag)
        {
            try
            {
                // Check the report contents are valid, but don't do anything else with it for now
                JsonConvert.DeserializeObject<IEnumerable<Failure>>(message.Report);
            }
            catch (JsonException e)
            {
                ErrorAndNack(header, tag, "Could not deserialize message report to Failure object", e);
                return;
            }

            try
            {
                _store.PersistMessageToStore(message, header);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage
                ErrorAndNack(header, tag, "Error while processing ExtractedFileVerificationMessage", e);
                return;
            }

            // TODO(rkm 2020-07-23) Forgetting the "return" in either case above could mean that the message gets ackd - can we rearrange the logic to avoid this?
            Ack(header, tag);
        }
    }
}
