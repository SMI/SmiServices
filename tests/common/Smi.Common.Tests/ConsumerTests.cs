
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
using System.Text.Json;
using System.Threading;

namespace Smi.Common.Tests
{
    [TestFixture]
    public class ConsumerTests
    {
        [Test]
        public void Consumer_UnhandledException_TriggersFatal()
        {
            var consumer = new TestConsumer();
            consumer.SetModel(Mock.Of<IModel>());

            var fatalCalled = false;
            consumer.OnFatal += (sender, args) => fatalCalled = true;

            consumer.TestMessage(new NullMessage());

            Assert.True(fatalCalled);
        }

    }

    public class NullMessage : IMessage
    {

    }

    public class TestConsumer : Consumer<NullMessage>
    {
        protected override void ProcessMessageImpl(IMessageHeader header, NullMessage msg, ulong tag)
        {
            throw new Exception("Throwing to trigger Fatal");
        }
    }

    public class DoNothingConsumer : Consumer<NullMessage>
    {
        protected override void ProcessMessageImpl(IMessageHeader header, NullMessage msg, ulong tag)
        {

        }
    }

    public class SelfClosingConsumer : Consumer<NullMessage>
    {
        protected override void ProcessMessageImpl(IMessageHeader header, NullMessage msg, ulong tag)
        {
            Model.Close();
        }
    }

}
