
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;

namespace Smi.Common.Tests
{
    [RequiresRabbit]
    public class HeaderPreservationTest
    {
        [Test]
        public void SendHeader()
        {
            var o = new GlobalOptionsFactory().Load(nameof(SendHeader));

            var consumerOptions = new ConsumerOptions
            {
                QueueName = "TEST.HeaderPreservationTest_Read1",
                AutoAck = false,
                QoSPrefetchCount = 1
            };

            TestConsumer consumer;

            using var tester = new MicroserviceTester(o.RabbitOptions, consumerOptions);

            var header = new MessageHeader {
                MessageGuid = Guid.Parse("5afce68f-c270-4bf3-b327-756f6038bb76"),
                Parents = new[] { Guid.Parse("12345678-c270-4bf3-b327-756f6038bb76"), Guid.Parse("87654321-c270-4bf3-b327-756f6038bb76") },
            };

            tester.SendMessage(consumerOptions, header, new TestMessage { Message = "hi" });

            consumer = new TestConsumer();
            tester.Adapter.StartConsumer(consumerOptions, consumer);

            TestTimelineAwaiter.Await(() => consumer.Failed || consumer.Passed, "timed out", 5000);

            Assert.IsTrue(consumer.Passed);
        }

        private class TestConsumer : Consumer<TestMessage>
        {
            public bool Passed { get; private set; }
            public bool Failed { get; private set; }


            protected override void ProcessMessageImpl(IMessageHeader? header, TestMessage message, ulong tag)
            {
                try
                {
                    Assert.AreEqual(header.Parents[0].ToString(), "12345678-c270-4bf3-b327-756f6038bb76");
                    Assert.AreEqual(header.Parents[1].ToString(), "87654321-c270-4bf3-b327-756f6038bb76");
                    Assert.AreEqual(header.Parents[2].ToString(), "5afce68f-c270-4bf3-b327-756f6038bb76");

                    Passed = true;
                    Ack(header,tag);
                }
                catch (Exception)
                {
                    Failed = true;
                }
            }
        }

        private class TestMessage : IMessage
        {
            public string? Message { get; set; }
        }
    }

}
