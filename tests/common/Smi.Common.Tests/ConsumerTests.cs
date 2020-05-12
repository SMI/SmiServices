﻿
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace Smi.Common.Tests
{
    [TestFixture]
    public class ConsumerTests
    {
        [Test]
        public void Consumer_UnhandledException_TriggersFatal()
        {
            var mockDeliverArgs = Mock.Of<BasicDeliverEventArgs>();
            mockDeliverArgs.DeliveryTag = 1;
            mockDeliverArgs.BasicProperties = new BasicProperties { Headers = new Dictionary<string, object>() };
            var header = new MessageHeader();
            header.Populate(mockDeliverArgs.BasicProperties.Headers);
            mockDeliverArgs.BasicProperties.Headers["MessageGuid"] = Encoding.UTF8.GetBytes(header.MessageGuid.ToString());
            mockDeliverArgs.BasicProperties.Headers["ProducerExecutableName"] = Encoding.UTF8.GetBytes(header.ProducerExecutableName);
            mockDeliverArgs.BasicProperties.Headers["Parents"] = Encoding.UTF8.GetBytes(string.Join("->", header.Parents));

            var consumer = new TestConsumer();
            consumer.SetModel(Mock.Of<IModel>());

            var fatalCalled = false;
            consumer.OnFatal += (sender, args) => fatalCalled = true;

            consumer.ProcessMessage(mockDeliverArgs);

            Thread.Sleep(1000);
            Assert.True(fatalCalled);
        }

    }

    // Dummy IMessage for message testing purposes
    public class TestMessage : IMessage
    {

    }

    public class TestConsumer : Consumer<TestMessage>
    {
        protected override void ProcessMessageImpl(IMessageHeader header, TestMessage msg, ulong tag)
        {
            throw new Exception("Throwing to trigger Fatal");
        }
    }

    public class DoNothingConsumer : Consumer<TestMessage>
    {
        protected override void ProcessMessageImpl(IMessageHeader header, TestMessage msg, ulong tag)
        {

        }
    }

    public class SelfClosingConsumer : Consumer<TestMessage>
    {
        protected override void ProcessMessageImpl(IMessageHeader header, TestMessage msg, ulong tag)
        {
            Model.Close();
        }
    }

}
