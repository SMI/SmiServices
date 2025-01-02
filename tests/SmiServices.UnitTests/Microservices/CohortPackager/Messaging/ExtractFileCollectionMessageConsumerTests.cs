using Moq;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortPackager;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using System;

namespace SmiServices.UnitTests.Microservices.CohortPackager.Messaging;

internal class ExtractFileCollectionMessageConsumerTests
{
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
        jobStoreMock.Setup(x => x.PersistMessageToStore(It.IsAny<ExtractFileCollectionInfoMessage>(), It.IsAny<IMessageHeader>()));

        var consumer = new ExtractFileCollectionMessageConsumer(jobStoreMock.Object);

        var message = new ExtractFileCollectionInfoMessage();

        // Act

        consumer.ProcessMessage(new MessageHeader(), message, 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(consumer.AckCount, Is.EqualTo(1));
            Assert.That(consumer.NackCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void ProcessMessageImpl_HandlesApplicationException()
    {
        // Arrange

        var jobStoreMock = new Mock<IExtractJobStore>(MockBehavior.Strict);
        jobStoreMock
            .Setup(x => x.PersistMessageToStore(It.IsAny<ExtractFileCollectionInfoMessage>(), It.IsAny<IMessageHeader>()))
            .Throws(new ApplicationException("Some error..."));

        var consumer = new ExtractFileCollectionMessageConsumer(jobStoreMock.Object);

        var message = new ExtractFileCollectionInfoMessage();

        // Act

        consumer.ProcessMessage(new MessageHeader(), message, 1);

        Assert.Multiple(() =>
        {
            // Assert

            Assert.That(consumer.AckCount, Is.EqualTo(0));
            Assert.That(consumer.NackCount, Is.EqualTo(1));
        });
    }

    #endregion
}
