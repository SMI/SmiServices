using Smi.Common.Messages;
using Smi.Common.Messaging;
using RabbitMQ.Client.Events;
using System.Text;
using System;
using System.Collections.Generic;
using NLog;

namespace Smi.Common.Tests.DeadLetterMessagingTests
{
    /// <summary>
    /// Angry consumer that rejects any messages sent to it!
    /// </summary>
    public class MessageRejector : Consumer<IMessage>
    {
        public bool AcceptNext { get; set; }

        public IMessageHeader LastHeader { get; private set; }
        public ulong LastTag { get; private set; }
        public BasicDeliverEventArgs LastDeliverArgs { get; private set; }

        public override void ProcessMessage(BasicDeliverEventArgs deliverArgs)
        {
            LastDeliverArgs = deliverArgs;
            Encoding enc = Encoding.UTF8;
            MessageHeader header = null;

            try
            {
                if (deliverArgs.BasicProperties != null)
                {
                    if (deliverArgs.BasicProperties.ContentEncoding != null)
                        enc = Encoding.GetEncoding(deliverArgs.BasicProperties.ContentEncoding);

                    header = new MessageHeader(deliverArgs.BasicProperties == null ? new Dictionary<string, object>() : deliverArgs.BasicProperties.Headers, enc);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Message header content was null, or could not be parsed into a MessageHeader object: " + e);
                Model.BasicNack(deliverArgs.DeliveryTag, false, false);
                return;
            }
            ProcessMessageImpl(header, null, deliverArgs.DeliveryTag);
        }

        protected override void ProcessMessageImpl(IMessageHeader header, IMessage message, ulong tag)
        {
            LastHeader = header;
            LastTag = tag;

            if (AcceptNext)
            {
                Ack(header, tag);
                AcceptNext = false;
                return;
            }

            ErrorAndNack(header, tag, "Message rejected!", null);
        }
    }
}
