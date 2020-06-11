#nullable enable
using System;
using System.Collections.Generic;
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
        /// Count of the messages Acknowledged by this Consumer, use <see cref="Ack"/> to increment this
        /// </summary>
        public int AckCount { get; private set; }

        /// <summary>
        /// Count of the messages Rejected by this Consumer, use <see cref="ErrorAndNack"/> to increment this
        /// </summary>
        public int NackCount { get; private set; }

        /// <summary>
        /// Event raised when Fatal method called
        /// </summary>
        public event ConsumerFatalHandler? OnFatal;

        /// <summary>
        /// Why the last Nack was sent
        /// </summary>
        public string? lastnackreason { get; private set; }
        public uint MessageCount { get; set; }

        private readonly List<ulong> _retriedtags = new List<ulong>();

        protected readonly ILogger Logger;

        private readonly object _oConsumeLock = new object();
        private bool _exiting;

        protected Acker? Model;

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


        public void SetModel(Acker model)
        {
            Model = model;
        }

        public virtual void ProcessMessage(BasicDeliverEventArgs deliverArgs, byte[] msg)
        {
            lock (_oConsumeLock)
            {
                if (_exiting)
                    return;
            }

            // If we did not receive a valid header, ditch the message and continue.
            // Control messages (no header) are handled in their own ProcessMessage implementation

            Encoding enc = Encoding.UTF8;
            MessageHeader? header=null;

            try
            {
                if (deliverArgs.BasicProperties != null)
                {
                    if (deliverArgs.BasicProperties.ContentEncoding != null)
                        enc = Encoding.GetEncoding(deliverArgs.BasicProperties.ContentEncoding);

                    header = new MessageHeader(deliverArgs.BasicProperties == null ? new Dictionary<string, object>() : deliverArgs.BasicProperties.Headers, enc);
                    header.Log(Logger, LogLevel.Trace, "Received");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Message header content was null, or could not be parsed into a MessageHeader object: " + e);

                DiscardSingleMessage(deliverArgs.DeliveryTag);

                return;
            }

            // Now pass the message on to the implementation, catching and calling Fatal on any unhandled exception

            try
            {
                SafeDeserializeToMessage<TMessage>(header, deliverArgs, msg, out TMessage message);
                ProcessMessageImpl(header, message, deliverArgs.DeliveryTag);
            }
            catch (Newtonsoft.Json.JsonSerializationException e)
            {
                // Deserialization exception - Can never process this message
                Logger.Debug("JsonSerializationException, doing ErrorAndNack for message (DeliveryTag " + deliverArgs.DeliveryTag + ")");
                if (_retriedtags.Contains(deliverArgs.DeliveryTag)) 
                ErrorAndNack(header, deliverArgs.DeliveryTag, $"JSON error '{e}' - {e.Data})", e);
                else
                {
                    // Trigger a single retry to guard against Rabbit frame corruption.
                    _retriedtags.Add(deliverArgs.DeliveryTag);
                    Model?.BasicNack(deliverArgs.DeliveryTag, false, true);
                }
            }
            catch (Exception e)
            {
                Fatal("ProcessMessageImpl threw unhandled exception", e);
            }
        }


        protected abstract void ProcessMessageImpl(IMessageHeader? header, TMessage message, ulong tag);

        public void TestMessage(TMessage message,IMessageHeader? header=null)
        {
            try
            {
                ProcessMessageImpl(header, message, 0);
            } catch (Exception e)
            {
                Fatal("ProcessMessageImpl threw unhandled exception", e);
            }
        }

        /// <summary>
        /// Safely deserialize a <see cref="BasicDeliverEventArgs"/> to an <see cref="IMessage"/>. Returns true if the deserialization
        /// was successful (message available from the out parameter), or false (out iMessage is null)
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <param name="deliverArgs"></param>
        /// <param name="iMessage"></param>
        /// <returns></returns>
        /// </summary>
        protected void SafeDeserializeToMessage<T>(IMessageHeader? header, BasicDeliverEventArgs deliverArgs, byte[] msg, out T iMessage) where T : IMessage?
        {
            iMessage = JsonConvert.DeserializeObject<T>(msg);
        }

        /// <summary>
        /// Instructs RabbitMQ to discard a single message and not requeue it
        /// </summary>
        /// <param name="tag"></param>
        protected void DiscardSingleMessage(ulong tag)
        {
            Model?.BasicNack(tag, multiple: false, requeue: false);
            NackCount++;
        }

        protected virtual void ErrorAndNack(IMessageHeader? header, ulong tag, string? message, Exception? exception)
        {
            lastnackreason = message;
            if (header != null)
                header.Log(Logger, LogLevel.Error, message, exception);

            DiscardSingleMessage(tag);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="deliverEventArgs"></param>
        protected void Ack(IMessageHeader? header, ulong tag)
        {
            if (header != null)
                header.Log(Logger, LogLevel.Trace, "Acknowledged");

            Model?.BasicAck(tag, false);
            AckCount++;
        }

        /// <summary>
        /// Acknowledges all in batch, this uses multiple which means you are accepting all up to the last message in the batch (including any not in your list
        /// for any reason)
        /// </summary>
        /// <param name="batchHeaders"></param>
        /// <param name="latestDeliveryTag"></param>
        protected void Ack(IList<IMessageHeader> batchHeaders, ulong latestDeliveryTag)
        {
            foreach (IMessageHeader header in batchHeaders)
                header.Log(Logger, LogLevel.Trace, "Acknowledged");

            Model?.BasicAck(latestDeliveryTag, true);
            AckCount += batchHeaders.Count;
        }

        /// <summary>
        /// Logs a Fatal in the Logger and triggers the FatalError event which should shutdown the RabbitMQAdapter
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

                ConsumerFatalHandler? onFatal = OnFatal;

                if (onFatal != null)
                {
                    onFatal.Invoke(this, new FatalErrorEventArgs(msg, exception));
                }
                else
                {
                    Logger.Fatal(exception, msg);
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
