using Smi.Common.Messages;
using Smi.Common.Messaging;
using RabbitMQ.Client.Events;

namespace Smi.Common.Tests.DeadLetterMessagingTests
{
    /// <summary>
    /// Angry consumer that rejects any messages sent to it!
    /// </summary>
    public class MessageRejector : Consumer
    {
        public bool AcceptNext { get; set; }

        public IMessageHeader LastHeader { get; private set; }
        public BasicDeliverEventArgs LastArgs { get; private set; }


        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs deliverArgs)
        {
            LastHeader = header;
            LastArgs = deliverArgs;

            if (AcceptNext)
            {
                Ack(header,  deliverArgs);

                AcceptNext = false;
                return;
            }

            ErrorAndNack(header,  deliverArgs, "Message rejected!", null);
        }
    }
}
