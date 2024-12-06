using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using SmiServices.Common.Messages;
using SmiServices.Common.Messaging;
using System;
using System.Collections.Generic;

namespace SmiServices.UnitTests.Common.Messaging;

internal class ProducerModelTests
{
    private class TestMessage : IMessage { }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [Test]
    public void SendMessage_HappyPath()
    {
        // Arrange
        bool timedOut = false;
        var mockModel = new Mock<IChannel>(MockBehavior.Strict);
        mockModel.Setup(x => x.BasicPublish("Exchange", "", true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
        mockModel.Setup(x => x.WaitForConfirms(It.IsAny<TimeSpan>(), out timedOut)).Returns(true);

        var mockBasicProperties = new Mock<IBasicProperties>();
        mockBasicProperties.Setup(x => x.Headers).Returns(new Dictionary<string, object>());

        var mockBackoffProvider = new Mock<IBackoffProvider>(MockBehavior.Strict);
        mockBackoffProvider.Setup(x => x.GetNextBackoff()).Returns(TimeSpan.Zero);
        mockBackoffProvider.Setup(x => x.Reset()).Verifiable();

        var maxRetryAttempts = 1;
        var producerModel = new ProducerModel("Exchange", mockModel.Object, mockBasicProperties.Object, maxRetryAttempts, mockBackoffProvider.Object);
        var message = new TestMessage();

        // Act
        // Assert
        Assert.DoesNotThrow(() => producerModel.SendMessage(message, inResponseTo: null, routingKey: null));
        mockBackoffProvider.Verify();
    }

    [Test]
    public void SendMessage_ThrowsException_OnTimeout()
    {
        // Arrange
        bool timedOut = true;
        var mockModel = new Mock<IChannel>(MockBehavior.Strict);
        mockModel.Setup(x => x.BasicPublish("Exchange", "", true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
        mockModel.Setup(x => x.WaitForConfirms(It.IsAny<TimeSpan>(), out timedOut)).Returns(false);

        var mockBasicProperties = new Mock<IBasicProperties>();
        mockBasicProperties.Setup(x => x.Headers).Returns(new Dictionary<string, object>());

        var mockBackoffProvider = new Mock<IBackoffProvider>(MockBehavior.Strict);
        mockBackoffProvider.Setup(x => x.GetNextBackoff()).Returns(TimeSpan.Zero);

        var maxRetryAttempts = 1;
        var producerModel = new ProducerModel("Exchange", mockModel.Object, mockBasicProperties.Object, maxRetryAttempts, mockBackoffProvider.Object);
        var message = new TestMessage();

        // Act
        // Assert
        var exc = Assert.Throws<ApplicationException>(() => producerModel.SendMessage(message, inResponseTo: null, routingKey: null));
        Assert.That(exc?.Message, Is.EqualTo("Could not confirm message published after timeout"));
    }

    [Test]
    public void SendMessage_WithSenseQueue_HappyPath()
    {
        // Arrange
        bool timedOut = false;
        var mockModel = new Mock<IChannel>(MockBehavior.Strict);
        mockModel.Setup(x => x.BasicPublish("Exchange", "", true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
        mockModel.Setup(x => x.WaitForConfirms(It.IsAny<TimeSpan>(), out timedOut)).Returns(true);
        mockModel.Setup(x => x.MessageCount("ProbeQueue")).Returns(0);

        var mockBasicProperties = new Mock<IBasicProperties>();
        mockBasicProperties.Setup(x => x.Headers).Returns(new Dictionary<string, object>());

        var mockBackoffProvider = new Mock<IBackoffProvider>(MockBehavior.Strict);
        mockBackoffProvider.Setup(x => x.GetNextBackoff()).Returns(TimeSpan.Zero);
        mockBackoffProvider.Setup(x => x.Reset()).Verifiable();

        var maxRetryAttempts = 1;
        var producerModel = new ProducerModel("Exchange", mockModel.Object, mockBasicProperties.Object, maxRetryAttempts, mockBackoffProvider.Object, "ProbeQueue", 1);
        var message = new TestMessage();

        // Act
        // Assert
        Assert.DoesNotThrow(() => producerModel.SendMessage(message, inResponseTo: null, routingKey: null));
        mockBackoffProvider.Verify();
    }

    [Test]
    public void SendMessage_WithSenseQueue_OverLimit()
    {
        // Arrange
        bool timedOut = false;
        var mockModel = new Mock<IChannel>(MockBehavior.Strict);
        mockModel.Setup(x => x.BasicPublish("Exchange", "", true, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
        mockModel.Setup(x => x.WaitForConfirms(It.IsAny<TimeSpan>(), out timedOut)).Returns(true);
        mockModel.SetupSequence(x => x.MessageCount("ProbeQueue"))
            .Returns(123)           // Check in constructor
            .Returns(123)           // First SendMessage call - first check
            .Returns(0)             // First SendMessage call - second check
            .Throws<Exception>();   // Throw if called again in second SendMessage call

        var mockBasicProperties = new Mock<IBasicProperties>();
        mockBasicProperties.Setup(x => x.Headers).Returns(() => new Dictionary<string, object>());

        var mockBackoffProvider = new Mock<IBackoffProvider>(MockBehavior.Strict);
        mockBackoffProvider.Setup(x => x.GetNextBackoff()).Returns(TimeSpan.Zero);
        mockBackoffProvider.Setup(x => x.Reset()).Verifiable();

        var maxRetryAttempts = 1;
        var producerModel = new ProducerModel("Exchange", mockModel.Object, mockBasicProperties.Object, maxRetryAttempts, mockBackoffProvider.Object, "ProbeQueue", 1, TimeSpan.Zero);
        var message = new TestMessage();

        // Act
        // Assert
        Assert.DoesNotThrow(() => producerModel.SendMessage(message, inResponseTo: null, routingKey: null));
        Assert.DoesNotThrow(() => producerModel.SendMessage(message, inResponseTo: null, routingKey: null));
        mockBackoffProvider.Verify();
    }
}
