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

internal class AnonFailedMessageConsumerTests
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
    public void ProcessMessageImpl_HappyPath()
    {
        // Arrange

        var jobStoreMock = new Mock<IExtractJobStore>(MockBehavior.Strict);
        jobStoreMock.Setup(x => x.PersistMessageToStore(It.IsAny<ExtractedFileStatusMessage>(), It.IsAny<IMessageHeader>()));

        var consumer = new AnonFailedMessageConsumer(jobStoreMock.Object);
        consumer.SetModel(new Mock<IModel>(MockBehavior.Loose).Object);

        var message = new ExtractedFileStatusMessage
        {
            DicomFilePath = "1/2/3.dcm",
            Status = ExtractedFileStatus.Anonymised,
            OutputFilePath = "1/2/3-an.dcm",
            StatusMessage = null,
        };

        // Act

        consumer.TestMessage(message);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(consumer.AckCount,Is.EqualTo(1));
            Assert.That(consumer.NackCount,Is.EqualTo(0));
        });
    }

    [Test]
    public void ProcessMessageImpl_HandlesApplicationException()
    {
        // Arrange

        var jobStoreMock = new Mock<IExtractJobStore>(MockBehavior.Strict);
        jobStoreMock
            .Setup(x => x.PersistMessageToStore(It.IsAny<ExtractedFileStatusMessage>(), It.IsAny<IMessageHeader>()))
            .Throws(new ApplicationException("Some error..."));

        var consumer = new AnonFailedMessageConsumer(jobStoreMock.Object);
        consumer.SetModel(new Mock<IModel>(MockBehavior.Loose).Object);

        var message = new ExtractedFileStatusMessage
        {
            DicomFilePath = "1/2/3.dcm",
            Status = ExtractedFileStatus.Anonymised,
            OutputFilePath = "1/2/3-an.dcm",
            StatusMessage = null,
        };

        // Act

        consumer.TestMessage(message);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(consumer.AckCount,Is.EqualTo(0));
            Assert.That(consumer.NackCount,Is.EqualTo(1));
        });
    }

    #endregion
}
