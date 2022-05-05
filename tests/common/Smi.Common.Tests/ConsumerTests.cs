using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using System;
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

            var fatalCalled = false;
            consumer.OnFatal += (sender, args) => fatalCalled = true;

            consumer.TestMessage(new TestMessage());

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
