using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Messaging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microservices.CohortPackager.Tests.Messaging;

internal class AnonVerificationMessageConsumerTests
{
    #region Fixture Methods

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestLogger.Setup();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    #endregion

    #region Test Methods

    [SetUp]
    public void SetUp() { }

    [TearDown]
    public void TearDown() { }

    #endregion

    #region Tests

    [Test]
    public void ProcessMessage_HandlesQueueLimit()
    {
        // Arrange

        var writeQueueCount = 0;
        var processedList = new ConcurrentQueue<Tuple<IMessageHeader, ulong>>();
        var queueLimit = 2;

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore
            .Setup(x => x.AddToWriteQueue(It.IsAny<ExtractedFileVerificationMessage>(), It.IsAny<IMessageHeader>(), It.IsAny<ulong>()))
            .Callback(() => ++writeQueueCount);
        mockJobStore
            .Setup(x => x.ProcessedVerificationMessages)
            .Returns(processedList);
        mockJobStore
            .Setup(x => x.ProcessVerificationMessageQueue())
            .Callback(() =>
            {
                while (writeQueueCount > 0)
                {
                    processedList.Enqueue(new Tuple<IMessageHeader, ulong>(null!, 0));
                    --writeQueueCount;
                }
            });

        var consumer = new AnonVerificationMessageConsumer(mockJobStore.Object, processBatches: true, queueLimit, TimeSpan.MaxValue);
        consumer.SetModel(new Mock<IModel>(MockBehavior.Loose).Object);

        var message = new ExtractedFileVerificationMessage
        {
            Report = "[]",
        };

        // Act

        consumer.TestMessage(message);

        // Assert

        Assert.AreEqual(1, writeQueueCount);
        Assert.AreEqual(0, consumer.AckCount);

        // Act

        consumer.TestMessage(message);

        // Assert

        Assert.AreEqual(0, writeQueueCount);
        Assert.AreEqual(2, consumer.AckCount);
    }

    [Test]
    public void QueueTimer()
    {
        // Arrange

        var writeQueueCount = 0;
        var queueLimit = 2;
        var queueTime = TimeSpan.FromSeconds(1);
        var processedList = new ConcurrentQueue<Tuple<IMessageHeader, ulong>>();

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore
            .Setup(x => x.AddToWriteQueue(It.IsAny<ExtractedFileVerificationMessage>(), It.IsAny<IMessageHeader>(), It.IsAny<ulong>()))
            .Callback(() => ++writeQueueCount);
        mockJobStore
            .Setup(x => x.ProcessedVerificationMessages)
            .Returns(processedList);
        mockJobStore
            .Setup(x => x.ProcessVerificationMessageQueue())
            .Callback(() =>
            {
                while (writeQueueCount > 0)
                {
                    processedList.Enqueue(new Tuple<IMessageHeader, ulong>(null!, 0));
                    --writeQueueCount;
                }
            });

        var consumer = new AnonVerificationMessageConsumer(mockJobStore.Object, processBatches: true, queueLimit, queueTime);
        consumer.SetModel(new Mock<IModel>(MockBehavior.Loose).Object);

        var message = new ExtractedFileVerificationMessage
        {
            Report = "[]",
        };

        // Act

        consumer.TestMessage(message);

        // Wait for the timer to elapse, with a bit of wiggle room
        var t = 0;
        var checkInterval = 10;
        while (t < queueTime.TotalMilliseconds + 500)
        {
            if (consumer.AckCount == 1)
                break;

            t += checkInterval;
            Thread.Sleep(checkInterval);
        }

        // Assert

        Assert.AreEqual(0, writeQueueCount);
        Assert.AreEqual(1, consumer.AckCount);
    }

    #endregion
}
