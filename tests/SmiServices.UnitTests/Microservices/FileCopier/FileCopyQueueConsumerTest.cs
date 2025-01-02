using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.FileCopier;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.TestCommon;
using System;


namespace SmiServices.UnitTests.Microservices.FileCopier;

public class FileCopyQueueConsumerTest
{
    #region Fixture Methods 

    private ExtractFileMessage _message = null!;
    private Mock<IFileCopier> _mockFileCopier = null!;

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
        _message = new ExtractFileMessage
        {
            JobSubmittedAt = DateTime.UtcNow,
            ExtractionJobIdentifier = Guid.NewGuid(),
            ProjectNumber = "1234",
            ExtractionDirectory = "foo",
            DicomFilePath = "foo.dcm",
            IsIdentifiableExtraction = true,
            OutputPath = "bar",
        };

        _mockFileCopier = new Mock<IFileCopier>(MockBehavior.Strict);
        _mockFileCopier.Setup(x => x.ProcessMessage(It.IsAny<ExtractFileMessage>(), It.IsAny<IMessageHeader>()));
    }

    [TearDown]
    public void TearDown() { }

    #endregion

    #region Tests

    [Test]
    public void Test_FileCopyQueueConsumer_ValidMessage_IsAcked()
    {
        var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);

        consumer.ProcessMessage(new MessageHeader(), _message, 1);

        TestTimelineAwaiter.Await(() => consumer.AckCount == 1 && consumer.NackCount == 0);
    }

    [Test]
    public void Test_FileCopyQueueConsumer_ApplicationException_IsNacked()
    {
        _mockFileCopier.Reset();
        _mockFileCopier.Setup(x => x.ProcessMessage(It.IsAny<ExtractFileMessage>(), It.IsAny<IMessageHeader>())).Throws<ApplicationException>();

        var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);

        consumer.ProcessMessage(new MessageHeader(), _message, 1);

        TestTimelineAwaiter.Await(() => consumer.AckCount == 0 && consumer.NackCount == 1);
    }

    [Test]
    public void Test_FileCopyQueueConsumer_UnknownException_CallsFatalCallback()
    {
        _mockFileCopier.Reset();
        _mockFileCopier.Setup(x => x.ProcessMessage(It.IsAny<ExtractFileMessage>(), It.IsAny<IMessageHeader>())).Throws<Exception>();

        var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);

        var fatalCalled = false;
        consumer.OnFatal += (sender, _) => fatalCalled = true;

        consumer.ProcessMessage(new MessageHeader(), _message, 1);

        TestTimelineAwaiter.Await(() => fatalCalled, "Expected Fatal to be called");
        Assert.Multiple(() =>
        {
            Assert.That(consumer.AckCount, Is.EqualTo(0));
            Assert.That(consumer.NackCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void Test_FileCopyQueueConsumer_AnonExtraction_ThrowsException()
    {
        _message.IsIdentifiableExtraction = false;

        _mockFileCopier.Reset();
        _mockFileCopier.Setup(x => x.ProcessMessage(It.IsAny<ExtractFileMessage>(), It.IsAny<IMessageHeader>())).Throws<Exception>();

        var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);

        var fatalCalled = false;
        consumer.OnFatal += (sender, _) => fatalCalled = true;

        consumer.ProcessMessage(new MessageHeader(), _message, 1);

        TestTimelineAwaiter.Await(() => fatalCalled, "Expected Fatal to be called");
        Assert.Multiple(() =>
        {
            Assert.That(consumer.AckCount, Is.EqualTo(0));
            Assert.That(consumer.NackCount, Is.EqualTo(0));
        });
    }

    #endregion
}
