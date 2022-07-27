using IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Service;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Microservices.IsIdentifiable.Tests.Service
{
    public class IsIdentifiableQueueConsumerTests
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
        public void Constructor_NullProducerModel_ThrowsException()
        {
            var exc = Assert.Throws<ArgumentNullException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    null,
                    "foo",
                    new Mock<IClassifier>().Object
                );
            });
            Assert.AreEqual("Value cannot be null. (Parameter 'producer')", exc.Message);
        }

        [Test]
        public void Constructor_NullOrWhitespaceExtractionRoot_ThrowsException()
        {
            var exc = Assert.Throws<ArgumentException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    new Mock<IProducerModel>().Object,
                    null,
                    new Mock<IClassifier>().Object
                );
            });
            Assert.AreEqual("Argument cannot be null or whitespace (Parameter 'extractionRoot')", exc.Message);

            exc = Assert.Throws<ArgumentException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    new Mock<IProducerModel>().Object,
                    "   ",
                    new Mock<IClassifier>().Object
                );
            });
            Assert.AreEqual("Argument cannot be null or whitespace (Parameter 'extractionRoot')", exc.Message);
        }

        [Test]
        public void Constructor_NullClassifier_ThrowsException()
        {
            var exc = Assert.Throws<ArgumentNullException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    new Mock<IProducerModel>().Object,
                    "foo",
                    null
                );
            });
            Assert.AreEqual("Value cannot be null. (Parameter 'classifier')", exc.Message);
        }

        [Test]
        public void Constructor_MissingExtractRoot_ThrowsException()
        {
            var mockFs = new MockFileSystem();

            var exc = Assert.Throws<DirectoryNotFoundException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                   new Mock<IProducerModel>().Object,
                   "foo",
                   new Mock<IClassifier>().Object,
                   mockFs
                );
            });
            Assert.AreEqual("Could not find the extraction root 'foo' in the filesystem", exc.Message);
        }

        [Test]
        public void Constructor_ValidExtractRoot_DoesNotThrowException()
        {
            var mockFs = new MockFileSystem();

            var dir = mockFs.DirectoryInfo.FromDirectoryName("foo");
            dir.Create();
            var _ = new IsIdentifiableQueueConsumer(
                   new Mock<IProducerModel>().Object,
                   dir.FullName,
                   new Mock<IClassifier>().Object,
                   mockFs
            );
        }

        [Test]
        public void ProcessMessage_HappyPath()
        {
            // Arrange

            var mockProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            mockProducerModel
                .Setup(
                    x => x.SendMessage(
                        It.IsAny<ExtractedFileVerificationMessage>(),
                        null,
                        ""
                    )
                )
                .Returns(new MessageHeader());

            var mockClassifier = new Mock<IClassifier>(MockBehavior.Strict);
            mockClassifier.Setup(x => x.Classify(It.IsAny<IFileInfo>())).Returns(new List<Failure>());

            var mockFs = new MockFileSystem();
            var mockExtractRoot = mockFs.Directory.CreateDirectory("extractRoot");
            var mockExtractionDir = mockFs.Directory.CreateDirectory($"{mockExtractRoot}/proj1/extractions/ex1");
            mockFs.AddFile($"{mockExtractionDir}/foo-an.dcm", null);

            var mockModel = new Mock<IModel>(MockBehavior.Strict);
            mockModel.Setup(x => x.IsClosed).Returns(false);
            mockModel.Setup(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));

            var consumer = new IsIdentifiableQueueConsumer(
                mockProducerModel.Object,
                mockExtractRoot.FullName,
                mockClassifier.Object,
                mockFs
            );
            consumer.SetModel(mockModel.Object);

            var message = new ExtractedFileStatusMessage
            {
                DicomFilePath = "/src/foo.dcm",
                Status = ExtractedFileStatus.Anonymised,
                ProjectNumber = "proj1",
                ExtractionDirectory = "proj1/extractions/ex1",
                OutputFilePath = "foo-an.dcm",
            };

            // Act

            consumer.TestMessage(message);

            // Assert

            Assert.AreEqual(1, consumer.AckCount);
        }

        #endregion
    }
}
