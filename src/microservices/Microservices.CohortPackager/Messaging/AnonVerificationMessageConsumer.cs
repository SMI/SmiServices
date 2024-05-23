using IsIdentifiable.Failures;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Newtonsoft.Json;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Timers;


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
        private readonly Timer _verificationStatusQueueTimer;
        private bool _ignoreNewMessages = false;
        private bool _queueIsProcessing = false;

        public AnonVerificationMessageConsumer(IExtractJobStore store, bool processBatches, int maxUnacknowledgedMessages, TimeSpan verificationMessageQueueFlushTime)
        {
            _store = store;
            _maxUnacknowledgedMessages = maxUnacknowledgedMessages;
            _processBatches = processBatches;

            // NOTE: Timer rejects values larger than int.MaxValue
            if (verificationMessageQueueFlushTime.TotalMilliseconds >= int.MaxValue)
                verificationMessageQueueFlushTime = TimeSpan.FromMilliseconds(int.MaxValue);

            _verificationStatusQueueTimer = new Timer(verificationMessageQueueFlushTime);

            _verificationStatusQueueTimer.Elapsed += TimerHandler;

            if (_processBatches)
            {
                Logger.Debug($"Starting {nameof(_verificationStatusQueueTimer)}");
                _verificationStatusQueueTimer.Start();
            }
        }

        private void TimerHandler(object? sender, ElapsedEventArgs args)
        {
            if (_queueIsProcessing)
                return;

            _queueIsProcessing = true;

            try
            {
                _store.ProcessVerificationMessageQueue();
                AckAvailableMessages();
            }
            catch (Exception e)
            {
                _ignoreNewMessages = true;
                _verificationStatusQueueTimer.Stop();
                Logger.Error(e);
            }
            finally
            {
                _queueIsProcessing = false;
            }
        }

        protected override void ProcessMessageImpl(IMessageHeader header, ExtractedFileVerificationMessage message, ulong tag)
        {
            if (_ignoreNewMessages)
                return;

            try
            {
                // Check the report contents are valid here, since we just treat it as a JSON string from now on
                _ = JsonConvert.DeserializeObject<IEnumerable<Failure>>(message.Report ?? throw new InvalidOperationException());
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
            _ignoreNewMessages = true;
            _verificationStatusQueueTimer.Stop();

            try
            {
                _store.ProcessVerificationMessageQueue();
                AckAvailableMessages();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Error when processing outstanding messages on Dispose. Some messages in the store may unacknowledged");
            }
        }
    }
}
