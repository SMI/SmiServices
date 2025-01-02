using Moq;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortPackager;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SmiServices.UnitTests.Microservices.CohortPackager.Messaging;

internal class AnonVerificationMessageConsumerTests
{
    private Mock<IExtractJobStore> _mockJobStore = new();
    private int _writeQueueCount;
    private ConcurrentQueue<Tuple<IMessageHeader, ulong>> _processedList = new();
    private static readonly ExtractedFileVerificationMessage _invalidMessage = new() { Report = "<invalid>", };
    private static readonly ExtractedFileVerificationMessage _emptyReportMessage = new() { Report = "[]", };

    #region Fixture Methods

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    #endregion

    #region Test Methods

    [SetUp]
    public void SetUp()
    {
        _writeQueueCount = 0;
        _processedList = new ConcurrentQueue<Tuple<IMessageHeader, ulong>>();
        _mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        _mockJobStore
            .Setup(x => x.AddToWriteQueue(It.IsAny<ExtractedFileVerificationMessage>(), It.IsAny<IMessageHeader>(), It.IsAny<ulong>()))
            .Callback(() => ++_writeQueueCount);
        _mockJobStore
            .Setup(x => x.ProcessedVerificationMessages)
            .Returns(_processedList);
        _mockJobStore
            .Setup(x => x.ProcessVerificationMessageQueue())
            .Callback(() =>
            {
                while (_writeQueueCount > 0)
                {
                    _processedList.Enqueue(new Tuple<IMessageHeader, ulong>(new MessageHeader(), 0));
                    --_writeQueueCount;
                }
            });
    }

    [TearDown]
    public void TearDown() { }

    private AnonVerificationMessageConsumer NewConsumer(bool processBatches, int maxUnacknowledgedMessages, TimeSpan verificationMessageQueueFlushTime)
    {
        var consumer = new AnonVerificationMessageConsumer(_mockJobStore.Object, processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);
        return consumer;
    }

    private static void SleepWithEarlyExit(TimeSpan verificationMessageQueueFlushTime, Func<bool> earlyExitCheck)
    {
        // Wait for the timer to elapse, with a bit of wiggle room
        var t = 0;
        var checkInterval = 10;
        while (t < verificationMessageQueueFlushTime.TotalMilliseconds + 500)
        {
            if (earlyExitCheck())
                return;

            t += checkInterval;
            Thread.Sleep(checkInterval);
        }
    }

    #endregion

    #region Tests

    [Test]
    public void ProcessMessage_HandlesInvalidReportContent()
    {
        // Arrange

        var processBatches = false;
        var maxUnacknowledgedMessages = 1;
        var verificationMessageQueueFlushTime = TimeSpan.MaxValue;
        var consumer = NewConsumer(processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);

        // Act

        consumer.ProcessMessage(new MessageHeader(), _invalidMessage, 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(consumer.AckCount, Is.EqualTo(0));
            Assert.That(consumer.NackCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProcessMessage_HandlesApplicationException()
    {
        // Arrange

        var processBatches = false;
        var maxUnacknowledgedMessages = 1;
        var verificationMessageQueueFlushTime = TimeSpan.MaxValue;
        var consumer = NewConsumer(processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);

        _mockJobStore
            .Setup(x => x.PersistMessageToStore(It.IsAny<ExtractedFileVerificationMessage>(), It.IsAny<IMessageHeader>()))
            .Throws(new ApplicationException("Oh no"));

        // Act

        consumer.ProcessMessage(new MessageHeader(), _emptyReportMessage, 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(consumer.AckCount, Is.EqualTo(0));
            Assert.That(consumer.NackCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProcessMessage_HandlesQueueLimit()
    {
        // Arrange

        var processBatches = true;
        var maxUnacknowledgedMessages = 2;
        var verificationMessageQueueFlushTime = TimeSpan.MaxValue;
        var consumer = NewConsumer(processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);

        // Act

        consumer.ProcessMessage(new MessageHeader(), _emptyReportMessage, 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(_writeQueueCount, Is.EqualTo(1));
            Assert.That(consumer.AckCount, Is.EqualTo(0));
        });

        // Act

        consumer.ProcessMessage(new MessageHeader(), _emptyReportMessage, 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(_writeQueueCount, Is.EqualTo(0));
            Assert.That(consumer.AckCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void QueueTimer_HappyPath()
    {
        // Arrange

        var processBatches = true;
        var maxUnacknowledgedMessages = 2;
        var verificationMessageQueueFlushTime = TimeSpan.FromSeconds(1);
        var consumer = NewConsumer(processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);

        // Act

        consumer.ProcessMessage(new MessageHeader(), _emptyReportMessage, 1);

        SleepWithEarlyExit(verificationMessageQueueFlushTime, () => consumer.AckCount == 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(_writeQueueCount, Is.EqualTo(0));
            Assert.That(consumer.AckCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void QueueTimer_HandlesException()
    {
        // Arrange

        var processBatches = true;
        var maxUnacknowledgedMessages = 2;
        var verificationMessageQueueFlushTime = TimeSpan.FromSeconds(1);
        var consumer = NewConsumer(processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);

        var hasThrown = false;
        _mockJobStore
            .Setup(x => x.ProcessVerificationMessageQueue())
            .Callback(() => { hasThrown = true; })
            .Throws(new Exception("Some error"));

        // Act

        consumer.ProcessMessage(new MessageHeader(), _emptyReportMessage, 1);

        SleepWithEarlyExit(verificationMessageQueueFlushTime, () => hasThrown);

        Thread.Sleep(100); // Allow time for exception handler to run and exit

        consumer.ProcessMessage(new MessageHeader(), _emptyReportMessage, 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(hasThrown, Is.True);
            Assert.That(_writeQueueCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void QueueTimer_DoesNotOverlap()
    {
        // Arrange

        var processBatches = true;
        var maxUnacknowledgedMessages = 2;
        var verificationMessageQueueFlushTime = TimeSpan.FromSeconds(0.1);
        var consumer = NewConsumer(processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);

        var timesCalled = 0;
        _mockJobStore
             .Setup(x => x.ProcessVerificationMessageQueue())
             // Simulate a long-running database call
             .Callback(() => { Thread.Sleep(200); ++timesCalled; })
             .Throws(new Exception("Some error"));

        // Act

        Thread.Sleep(10 * verificationMessageQueueFlushTime);

        // Assert

        Assert.That(timesCalled, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_HandlesException()
    {
        // Arrange

        var processBatches = true;
        var maxUnacknowledgedMessages = 2;
        var verificationMessageQueueFlushTime = TimeSpan.MaxValue;
        var consumer = NewConsumer(processBatches, maxUnacknowledgedMessages, verificationMessageQueueFlushTime);

        _mockJobStore
            .Setup(x => x.ProcessVerificationMessageQueue())
            .Throws(new Exception("Some error"));

        // Act

        consumer.Dispose();

        // Assert

        // No exception thrown
    }

    #endregion
}
