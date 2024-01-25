using IsIdentifiable.Failures;
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
    public sealed class AnonVerificationMessageConsumer : Consumer<ExtractedFileVerificationMessage>, IDisposable
    {
        private readonly IExtractJobStore _store;

        private readonly bool _processBatches;
        private readonly int _maxUnacknowledgedMessages;
        private int _unacknowledgedMessages = 0;


        public AnonVerificationMessageConsumer(IExtractJobStore store, bool processBatches, int maxUnacknowledgedMessages)
        {
            _store = store;
            _maxUnacknowledgedMessages = maxUnacknowledgedMessages;
            _processBatches = processBatches;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, ExtractedFileVerificationMessage message, ulong tag)
        {
            try
            {
                // Check the report contents are valid here, since we just treat it as a JSON string from now on
                _ = JsonConvert.DeserializeObject<IEnumerable<Failure>>(message.Report);
            }
            catch (JsonException e)
            {
                ErrorAndNack(header, tag, "Could not deserialize message report to Failure object", e);
                return;
            }

            try
            {
                if (_processBatches)
                    _store.AddToWriteQueue(message, header, tag);
                else
                    _store.PersistMessageToStore(message, header);
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper in ProcessMessage
                ErrorAndNack(header, tag, "Error while processing ExtractedFileVerificationMessage", e);
                return;
            }

            if (_processBatches)
            {
                if (++_unacknowledgedMessages >= _maxUnacknowledgedMessages)
                    _store.ProcessVerificationMessageQueue();
                AckAvailableMessages();
            }
            else
            {
                Ack(header, tag);
            }
        }

        private void AckAvailableMessages()
        {
            while (_store.ProcessedVerificationMessages.TryDequeue(out var processed))
            {
                Ack(processed.Item1, processed.Item2);
                _unacknowledgedMessages--;
            }
        }

        public void Dispose()
        {
            try
            {
                AckAvailableMessages();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Error when calling {nameof(AckAvailableMessages)} on Dispose. Some processed messages may unacknowledged");
            }
        }
    }
}
