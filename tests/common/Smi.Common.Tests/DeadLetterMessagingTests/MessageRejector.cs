using System;
using System.Text;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using RabbitMQ.Client.Events;

namespace Smi.Common.Tests.DeadLetterMessagingTests
{
    /// <summary>
    /// Angry consumer that rejects any messages sent to it!
    /// </summary>
    public class MessageRejector : Consumer<IMessage>
    {
        public bool AcceptNext { get; set; }

        public IMessageHeader LastHeader { get; private set; }
        public BasicDeliverEventArgs LastDeliverArgs { get; private set; }

        public override void ProcessMessage(BasicDeliverEventArgs ea, byte[] b)
        {
            Encoding enc = Encoding.UTF8;
            MessageHeader header;

            try
            {
                if (ea.BasicProperties.ContentEncoding != null)
                    enc = Encoding.GetEncoding(ea.BasicProperties.ContentEncoding);

                header = new MessageHeader(ea.BasicProperties.Headers, enc);
                header.Log(Logger, NLog.LogLevel.Trace, "Received");
            }
            catch (Exception e)
            {
                Logger.Error("Message header content was null, or could not be parsed into a MessageHeader object: " + e);

                DiscardSingleMessage(ea.DeliveryTag);
                
                return;
            }

            LastHeader = header;
            LastDeliverArgs = ea;
            if (AcceptNext)
            {
                Ack(header,ea.DeliveryTag);
                AcceptNext = false;
                return;
            }
            ErrorAndNack(header,ea.DeliveryTag,"Message rejected!",null);
        }

        protected override void ProcessMessageImpl(IMessageHeader header, IMessage message, ulong tag)
        {
            // Body now inlined above since this Consumer has non-standard behaviour
        }
    }
}
