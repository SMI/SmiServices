using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using System;
using System.Threading;


namespace SmiServices.UnitTests.Common
{
    public class ConsumerTests
    {
        [Test]
        public void Consumer_UnhandledException_TriggersFatal()
        {
            var consumer = new ThrowingConsumer();

            var fatalCalled = false;
            consumer.OnFatal += (sender, args) => fatalCalled = true;

            consumer.ProcessMessage(new MessageHeader(), new TestMessage(), 1);

            Thread.Sleep(1000);
            Assert.That(fatalCalled, Is.True);
        }

        [Test]
        public void MessageHolds()
        {
            // Arrange

            var consumer = new ThrowingConsumer()
            {
                HoldUnprocessableMessages = true,
                QoSPrefetchCount = 1,
            };

            // Act

            consumer.ProcessMessage(new MessageHeader(), new TestMessage(), 1);

            // Assert

            Assert.Multiple(() =>
            {
                Assert.That(consumer.HeldMessages, Is.EqualTo(1));
                Assert.That(consumer.AckCount, Is.EqualTo(0));
            });
        }
    }

    public class TestMessage : IMessage { }

    public class ThrowingConsumer : Consumer<TestMessage>
    {
        public int HeldMessages { get => _heldMessages; }

        protected override void ProcessMessageImpl(IMessageHeader header, TestMessage msg, ulong tag)
        {
            throw new Exception("Throwing!");
        }
    }
}
