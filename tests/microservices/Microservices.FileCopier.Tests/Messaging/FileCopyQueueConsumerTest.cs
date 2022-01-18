using Microservices.FileCopier.Execution;
using Microservices.FileCopier.Messaging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using Smi.Common.Tests.Messaging;
using System;


namespace Microservices.FileCopier.Tests.Messaging
{
    public class FileCopyQueueConsumerTest
    {
        #region Fixture Methods 

        private ExtractFileMessage _message;
        private Mock<IModel> _mockModel;
        private Mock<IFileCopier> _mockFileCopier;

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
            _mockModel = new Mock<IModel>(MockBehavior.Strict);
            _mockModel.Setup(x => x.IsClosed).Returns(false);
            _mockModel.Setup(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
            _mockModel.Setup(x => x.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()));

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
            BasicDeliverEventArgs mockDeliverArgs = ConsumerTestHelpers.GetMockDeliverArgs(_message);

            var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);
            consumer.SetModel(_mockModel.Object);

            consumer.ProcessMessage(mockDeliverArgs);

            new TestTimelineAwaiter().Await(() => consumer.AckCount == 1 && consumer.NackCount == 0);
        }

        [Test]
        public void Test_FileCopyQueueConsumer_ApplicationException_IsNacked()
        {
            BasicDeliverEventArgs mockDeliverArgs = ConsumerTestHelpers.GetMockDeliverArgs(_message);

            _mockFileCopier.Reset();
            _mockFileCopier.Setup(x => x.ProcessMessage(It.IsAny<ExtractFileMessage>(), It.IsAny<IMessageHeader>())).Throws<ApplicationException>();

            var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);
            consumer.SetModel(_mockModel.Object);

            consumer.ProcessMessage(mockDeliverArgs);

            new TestTimelineAwaiter().Await(() => consumer.AckCount == 0 && consumer.NackCount == 1);
        }

        [Test]
        public void Test_FileCopyQueueConsumer_UnknownException_CallsFatalCallback()
        {
            BasicDeliverEventArgs mockDeliverArgs = ConsumerTestHelpers.GetMockDeliverArgs(_message);

            _mockFileCopier.Reset();
            _mockFileCopier.Setup(x => x.ProcessMessage(It.IsAny<ExtractFileMessage>(), It.IsAny<IMessageHeader>())).Throws<Exception>();

            var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);
            consumer.SetModel(_mockModel.Object);

            var fatalCalled = false;
            consumer.OnFatal += (sender, _) => fatalCalled = true;

            consumer.ProcessMessage(mockDeliverArgs);

            new TestTimelineAwaiter().Await(() => fatalCalled, "Expected Fatal to be called");
            Assert.AreEqual(0, consumer.AckCount);
            Assert.AreEqual(0, consumer.NackCount);
        }

        [Test]
        public void Test_FileCopyQueueConsumer_AnonExtraction_ThrowsException()
        {
            _message.IsIdentifiableExtraction = false;
            BasicDeliverEventArgs mockDeliverArgs = ConsumerTestHelpers.GetMockDeliverArgs(_message);

            _mockFileCopier.Reset();
            _mockFileCopier.Setup(x => x.ProcessMessage(It.IsAny<ExtractFileMessage>(), It.IsAny<IMessageHeader>())).Throws<Exception>();

            var consumer = new FileCopyQueueConsumer(_mockFileCopier.Object);
            consumer.SetModel(_mockModel.Object);

            var fatalCalled = false;
            consumer.OnFatal += (sender, _) => fatalCalled = true;

            consumer.ProcessMessage(mockDeliverArgs);

            new TestTimelineAwaiter().Await(() => fatalCalled, "Expected Fatal to be called");
            Assert.AreEqual(0, consumer.AckCount);
            Assert.AreEqual(0, consumer.NackCount);
        }

        #endregion
    }
}
