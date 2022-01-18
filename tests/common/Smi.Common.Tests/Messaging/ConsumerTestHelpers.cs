using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Smi.Common.Messages;
using System.Collections.Generic;
using System.Text;

namespace Smi.Common.Tests.Messaging
{
    public static class ConsumerTestHelpers
    {
        public static BasicDeliverEventArgs GetMockDeliverArgs(IMessage message)
        {
            var mockDeliverArgs = Mock.Of<BasicDeliverEventArgs>(MockBehavior.Strict);
            mockDeliverArgs.DeliveryTag = 1;
            mockDeliverArgs.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            mockDeliverArgs.BasicProperties = new BasicProperties { Headers = new Dictionary<string, object>() };
            var header = new MessageHeader();
            header.Populate(mockDeliverArgs.BasicProperties.Headers);
            // Have to convert these to bytes since RabbitMQ normally does that when sending
            mockDeliverArgs.BasicProperties.Headers["MessageGuid"] = Encoding.UTF8.GetBytes(header.MessageGuid.ToString());
            mockDeliverArgs.BasicProperties.Headers["ProducerExecutableName"] = Encoding.UTF8.GetBytes(header.ProducerExecutableName);
            mockDeliverArgs.BasicProperties.Headers["Parents"] = Encoding.UTF8.GetBytes(string.Join("->", header.Parents));
            return mockDeliverArgs;
        }
    }
}
