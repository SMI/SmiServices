
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smi.Common.Events;
using Smi.Common.Messages;
using Smi.Common.MessageSerialization;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Smi.Common.Messaging
{
    public abstract class Consumer<TMessage> : IConsumer where TMessage : IMessage
    {
        /// <summary>
        /// Count of the messages Acknowledged by this Consumer, use <see cref="Ack(IMessageHeader, ulong)"/> to increment this
        /// </summary>
        public int AckCount { get; private set; }

        /// <summary>
        /// Count of the messages Rejected by this Consumer, use <see cref="ErrorAndNack"/> to increment this
        /// </summary>
        public int NackCount { get; private set; }

        /// <inheritdoc/>
        public bool HoldUnprocessableMessages { get; set; } = false;

        protected int _heldMessages = 0;

        /// <inheritdoc/>
        public int QoSPrefetchCount { get; set; }

        /// <summary>
        /// Event raised when Fatal method called
        /// </summary>
        public event ConsumerFatalHandler? OnFatal;


        protected readonly ILogger Logger;

        private readonly object _oConsumeLock = new();
        private bool _exiting;

        protected IModel? Model;

        public virtual void Shutdown()
        {

        }

        protected Consumer()
        {
            string loggerName;
            Type consumerType = GetType();

            if (consumerType.IsGenericType)
            {
                string namePrefix = consumerType.Name.Split(new[] { '`' }, StringSplitOptions.RemoveEmptyEntries)[0];
                IEnumerable<string> genericParameters = consumerType.GetGenericArguments().Select(x => x.Name);
                loggerName = $"{namePrefix}<{string.Join(",", genericParameters)}>";
            }
            else
            {
                loggerName = consumerType.Name;
            }

            Logger = LogManager.GetLogger(loggerName);
        }


        public void SetModel(IModel model)
        {
            if (model.IsClosed)
                throw new ArgumentException("Model is closed");

            Model = model;
        }

        public virtual void ProcessMessage(BasicDeliverEventArgs deliverArgs)
        {
            lock (_oConsumeLock)
            {
                if (_exiting)
                    return;
            }

            // Handled by RabbitMQ adapter in normal operation - only an issue in testing I think
            if (Model == null)
                throw new NullReferenceException("Model not set - use SetModel before processing messages");

            // If we did not receive a valid header, ditch the message and continue.
            // Control messages (no header) are handled in their own ProcessMessage implementation

            Encoding enc = Encoding.UTF8;
            MessageHeader header;

            try
            {
                if (deliverArgs.BasicProperties?.ContentEncoding != null)
                    enc = Encoding.GetEncoding(deliverArgs.BasicProperties.ContentEncoding);

                var headers = deliverArgs.BasicProperties?.Headers
                    ?? throw new ArgumentNullException("A part of deliverArgs.BasicProperties.Headers was null");

                header = new MessageHeader(headers, enc);
                header.Log(Logger, LogLevel.Trace, "Received");
            }
            catch (Exception e)
            {
                Logger.Error("Message header content was null, or could not be parsed into a MessageHeader object: " + e);

                DiscardSingleMessage(deliverArgs.DeliveryTag);

                return;
            }

            try
            {
                if (!SafeDeserializeToMessage<TMessage>(header, deliverArgs, out TMessage? message))
                    return;
                ProcessMessageImpl(header, message, deliverArgs.DeliveryTag);
            }
            catch (Exception e)
            {
                var messageBody = Encoding.UTF8.GetString(deliverArgs.Body.Span);
                Logger.Error(e, $"Unhandled exception when processing message {header.MessageGuid} with body: {messageBody}");

                if (HoldUnprocessableMessages)
                {
                    ++_heldMessages;
                    string msg = $"Holding an unprocessable message ({_heldMessages} total message(s) currently held";
                    if (_heldMessages >= QoSPrefetchCount)
                        msg += $". Have now exceeded the configured BasicQos value of {QoSPrefetchCount}. No further messages will be delivered to this consumer!";
                    Logger.Warn(msg);
                }
                else
                {
                    Fatal("ProcessMessageImpl threw unhandled exception", e);
                }
            }
        }

        public void TestMessage(TMessage msg)
        {
            try
            {
                ProcessMessageImpl(new MessageHeader(), msg, 1);
            }
            catch (Exception e)
            {
                Fatal("ProcessMessageImpl threw unhandled exception", e);
            }
        }


        protected abstract void ProcessMessageImpl(IMessageHeader header, TMessage message, ulong tag);

        /// <summary>
        /// Safely deserialize a <see cref="BasicDeliverEventArgs"/> to an <see cref="IMessage"/>. Returns true if the deserialization
        /// was successful (message available from the out parameter), or false (out iMessage is null)
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <param name="deliverArgs"></param>
        /// <param name="iMessage"></param>
        /// <returns></returns>
        /// </summary>
        protected bool SafeDeserializeToMessage<T>(IMessageHeader header, BasicDeliverEventArgs deliverArgs, [NotNullWhen(true)] out T? iMessage) where T : IMessage
        {
            try
            {
                iMessage = JsonConvert.DeserializeObject<T>(deliverArgs);
                return true;
            }
            catch (Newtonsoft.Json.JsonSerializationException e)
            {
                // Deserialization exception - Can never process this message

                Logger.Debug("JsonSerializationException, doing ErrorAndNack for message (DeliveryTag " + deliverArgs.DeliveryTag + ")");
                ErrorAndNack(header, deliverArgs.DeliveryTag, DeserializationMessage<T>(), e);

                iMessage = default;
                return false;
            }
        }

        /// <summary>
        /// Instructs RabbitMQ to discard a single message and not requeue it
        /// </summary>
        /// <param name="tag"></param>
        protected void DiscardSingleMessage(ulong tag)
        {
            Model!.BasicNack(tag, multiple: false, requeue: false);
            NackCount++;
        }

        protected virtual void ErrorAndNack(IMessageHeader header, ulong tag, string message, Exception exception)
        {
            if (header != null)
                header.Log(Logger, LogLevel.Error, message, exception);

            DiscardSingleMessage(tag);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="tag"></param>
        protected void Ack(IMessageHeader header, ulong tag)
        {
            header?.Log(Logger, LogLevel.Trace, $"Acknowledged {header.MessageGuid}");

            Model!.BasicAck(tag, false);
            AckCount++;
        }

        /// <summary>
        /// Acknowledges all in batch, this uses multiple which means you are accepting all up to the last message in the batch (including any not in your list
        /// for any reason)
        /// </summary>
        /// <param name="batchHeaders"></param>
        /// <param name="latestDeliveryTag"></param>
        protected void Ack(IEnumerable<IMessageHeader> batchHeaders, ulong latestDeliveryTag)
        {
            foreach (var header in batchHeaders)
            {
                header.Log(Logger, LogLevel.Trace, "Acknowledged");
                AckCount++;
            }

            Model.BasicAck(latestDeliveryTag, true);
        }

        /// <summary>
        /// Logs a Fatal in the Logger and triggers the FatalError event which should shutdown the MessageBroker
        /// <para>Do not do any further processing after triggering this method</para>
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="exception"></param>
        protected void Fatal(string msg, Exception exception)
        {
            lock (_oConsumeLock)
            {
                if (_exiting)
                    return;

                _exiting = true;

                Logger.Fatal(exception, msg);

                ConsumerFatalHandler? onFatal = OnFatal;

                if (onFatal != null)
                {
                    Task.Run(() => onFatal.Invoke(this, new FatalErrorEventArgs(msg, exception)));
                }
                else
                {
                    throw new Exception("No handlers when attempting to raise OnFatal for this exception", exception);
                }
            }
        }


        private static string DeserializationMessage<T>()
        {
            return "Could not deserialize message to " + typeof(T).Name + " object. Likely an issue with the message content";
        }
    }
}
