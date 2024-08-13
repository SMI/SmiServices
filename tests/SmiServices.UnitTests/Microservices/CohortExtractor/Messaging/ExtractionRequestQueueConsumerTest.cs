using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortExtractor;
using SmiServices.Microservices.CohortExtractor.Audit;
using SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SmiServices.UnitTests.Microservices.CohortExtractor.Messaging
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
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), null))
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
                ExtractionIdentifiers = ["foo"],
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
            Assert.That(fatalCalled, Is.False, $"Fatal was called with {fatalErrorEventArgs}");
            mockModel.Verify(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Once);
            Assert.That(fileMessageRoutingKey, Is.EqualTo(expectedRoutingKey));
        }

        #endregion
    }
}
