
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
    public abstract class Consumer : IConsumer
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
        public event ConsumerFatalHandler OnFatal;


        protected readonly ILogger Logger;

        private readonly object _oConsumeLock = new object();
        private bool _exiting;

        protected IModel Model;

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
                if (deliverArgs.BasicProperties.ContentEncoding != null)
                    enc = Encoding.GetEncoding(deliverArgs.BasicProperties.ContentEncoding);

                header = new MessageHeader(deliverArgs.BasicProperties.Headers, enc);
                header.Log(Logger, LogLevel.Trace, "Received");
            }
            catch (Exception e)
            {
                Logger.Error("Message header content was null, or could not be parsed into a MessageHeader object: " + e);

                Model.BasicNack(deliverArgs.DeliveryTag, false, false);
                ++NackCount;

                return;
            }

            // Now pass the message on to the implementation, catching and calling Fatal on any unhandled exception

            try
            {
                ProcessMessageImpl(header, deliverArgs);
            }
            catch (Exception e)
            {
                Fatal("ProcessMessageImpl threw unhandled exception", e);
            }
        }


        protected abstract void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs basicDeliverEventArgs);

        /// <summary>
        /// Safely deserialize a <see cref="BasicDeliverEventArgs"/> to an <see cref="IMessage"/>. Returns true if the deserialization
        /// was successful (message available from the out parameter), or false (out iMessage is null)
        /// <typeparam name="T"></typeparam>
        /// <param name="header"></param>
        /// <param name="deliverArgs"></param>
        /// <param name="iMessage"></param>
        /// <returns></returns>
        /// </summary>
        protected bool SafeDeserializeToMessage<T>(IMessageHeader header, BasicDeliverEventArgs deliverArgs, out T iMessage) where T : IMessage
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
                ErrorAndNack(header, deliverArgs, DeserializationMessage<T>(), e);

                iMessage = default(T);
                return false;
            }
        }

        protected virtual void ErrorAndNack(IMessageHeader header, BasicDeliverEventArgs deliverEventArgs, string message, Exception exception)
        {
            if (header != null)
                header.Log(Logger, LogLevel.Error, message, exception);

            Model.BasicNack(deliverEventArgs.DeliveryTag, false, false);
            NackCount++;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="deliverEventArgs"></param>
        protected void Ack(IMessageHeader header, BasicDeliverEventArgs deliverEventArgs)
        {
            if (header != null)
                header.Log(Logger, LogLevel.Trace, "Acknowledged");

            Model.BasicAck(deliverEventArgs.DeliveryTag, false);
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

            Model.BasicAck(latestDeliveryTag, true);
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

                ConsumerFatalHandler onFatal = OnFatal;

                if (onFatal != null)
                {
                    Task.Run(() => onFatal.Invoke(this, new FatalErrorEventArgs(msg, exception)));
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
