using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Messaging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using System;

namespace Microservices.CohortPackager.Tests.Messaging;

internal class ExtractionRequestInfoMessageConsumerTests
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
        jobStoreMock.Setup(x => x.PersistMessageToStore(It.IsAny<ExtractionRequestInfoMessage>(), It.IsAny<IMessageHeader>()));

        var consumer = new ExtractionRequestInfoMessageConsumer(jobStoreMock.Object);
        consumer.SetModel(new Mock<IModel>(MockBehavior.Loose).Object);

        var message = new ExtractionRequestInfoMessage();

        // Act

        consumer.TestMessage(message);

        // Assert

        Assert.AreEqual(1, consumer.AckCount);
        Assert.AreEqual(0, consumer.NackCount);
    }

    [Test]
    public void ProcessMessageImpl_HandlesApplicationException()
    {
        // Arrange

        var jobStoreMock = new Mock<IExtractJobStore>(MockBehavior.Strict);
        jobStoreMock
            .Setup(x => x.PersistMessageToStore(It.IsAny<ExtractionRequestInfoMessage>(), It.IsAny<IMessageHeader>()))
            .Throws(new ApplicationException("Some error..."));

        var consumer = new ExtractionRequestInfoMessageConsumer(jobStoreMock.Object);
        consumer.SetModel(new Mock<IModel>(MockBehavior.Loose).Object);

        var message = new ExtractionRequestInfoMessage();

        // Act

        consumer.TestMessage(message);

        // Assert

        Assert.AreEqual(0, consumer.AckCount);
        Assert.AreEqual(1, consumer.NackCount);
    }

    #endregion
}