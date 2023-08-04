using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.CohortExtractor.Messaging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microservices.CohortExtractor.Tests.Messaging
{
    public class ExtractionRequestQueueConsumerTest
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
        public void Test_ExtractionRequestQueueConsumer_AnonExtraction_RoutingKey()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Test_ExtractionRequestQueueConsumer_AnonExtraction_RoutingKey));
            globals.CohortExtractorOptions!.ExtractAnonRoutingKey = "anon";
            globals.CohortExtractorOptions.ExtractIdentRoutingKey = "";
            AssertMessagePublishedWithSpecifiedKey(globals, false, "anon");
        }

        [Test]
        public void Test_ExtractionRequestQueueConsumer_IdentExtraction_RoutingKey()
        {
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Test_ExtractionRequestQueueConsumer_IdentExtraction_RoutingKey));
            globals.CohortExtractorOptions!.ExtractAnonRoutingKey = "";
            globals.CohortExtractorOptions.ExtractIdentRoutingKey = "ident";
            AssertMessagePublishedWithSpecifiedKey(globals, true, "ident");
        }

        /// <summary>
        /// Checks that ExtractionRequestQueueConsumer publishes messages correctly according to the input message isIdentifiableExtraction value
        /// </summary>
        /// <param name="globals"></param>
        /// <param name="isIdentifiableExtraction"></param>
        /// <param name="expectedRoutingKey"></param>
        private static void AssertMessagePublishedWithSpecifiedKey(GlobalOptions globals, bool isIdentifiableExtraction, string expectedRoutingKey)
        {
            var fakeFulfiller = new FakeFulfiller();

            var mockFileMessageProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            string? fileMessageRoutingKey = null;
            mockFileMessageProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), It.IsNotNull<string>()))
                .Callback((IMessage _, IMessageHeader __, string routingKey) => { fileMessageRoutingKey = routingKey; })
                .Returns(new MessageHeader());
            mockFileMessageProducerModel.Setup(x => x.WaitForConfirms());

            var mockFileInfoMessageProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            mockFileInfoMessageProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), It.IsNotNull<string>()))
                .Returns(new MessageHeader());
            mockFileInfoMessageProducerModel.Setup(x => x.WaitForConfirms());

            var msg = new ExtractionRequestMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234",
                ExtractionDirectory = "1234/foo",
                IsIdentifiableExtraction = isIdentifiableExtraction,
                KeyTag = "foo",
                ExtractionIdentifiers = new List<string> { "foo" },
                Modalities = null,
            };

            var consumer = new ExtractionRequestQueueConsumer(
                globals.CohortExtractorOptions!,
                fakeFulfiller,
                new NullAuditExtractions(), new DefaultProjectPathResolver(),
                mockFileMessageProducerModel.Object,
                mockFileInfoMessageProducerModel.Object);

            var fatalCalled = false;
            FatalErrorEventArgs? fatalErrorEventArgs = null;
            consumer.OnFatal += (sender, args) =>
            {
                fatalCalled = true;
                fatalErrorEventArgs = args;
            };

            var mockModel = new Mock<IModel>(MockBehavior.Strict);
            mockModel.Setup(x => x.IsClosed).Returns(false);
            mockModel.Setup(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>())).Verifiable();

            consumer.SetModel(mockModel.Object);
            consumer.TestMessage(msg);

            Thread.Sleep(500); // Fatal call is race-y
            Assert.False(fatalCalled, $"Fatal was called with {fatalErrorEventArgs}");
            mockModel.Verify(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Once);
            Assert.AreEqual(expectedRoutingKey, fileMessageRoutingKey);
        }

        #endregion
    }
}
