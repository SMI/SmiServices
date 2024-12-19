using IsIdentifiable.Failures;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using SmiServices.Common.Events;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.IsIdentifiable;
using SmiServices.UnitTests.TestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using SmiServices.UnitTests.Common.Messaging;
using SmiServices.Common.Messaging;

namespace SmiServices.UnitTests.Microservices.IsIdentifiable
{
    public sealed class IsIdentifiableQueueConsumerTests
    {
        #region Fixture Methods

        private MockFileSystem _mockFs = null!;
        private IDirectoryInfo _extractRootDirInfo = null!;
        private string _extractDir = null!;
        private ExtractedFileStatusMessage _extractedFileStatusMessage = null!;
        private Mock<IModel> _mockModel = null!;
        private FatalErrorEventArgs? _fatalArgs;
        private readonly TestProducer<ExtractedFileVerificationMessage> _mockProducerModel = new();

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp()
        {
            _mockFs = new MockFileSystem();
            _extractRootDirInfo = _mockFs.Directory.CreateDirectory("extract");

            const string extractDirName = "extractDir";
            _extractDir = _mockFs.Path.Combine(_extractRootDirInfo.FullName, extractDirName);
            _mockFs.Directory.CreateDirectory(_extractDir);
            _mockFs.AddFile(_mockFs.Path.Combine(_extractDir, "foo-an.dcm"), null);

            _extractedFileStatusMessage = new ExtractedFileStatusMessage
            {
                DicomFilePath = "foo.dcm",
                Status = ExtractedFileStatus.Anonymised,
                ProjectNumber = "proj1",
                ExtractionDirectory = extractDirName,
                OutputFilePath = "foo-an.dcm",
            };

            _mockModel = new Mock<IModel>(MockBehavior.Strict);
            _mockModel.Setup(static x => x.IsClosed).Returns(false);
            _mockModel.Setup(static x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
            _mockModel.Setup(static x => x.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()));

            _fatalArgs = null;
        }

        [TearDown]
        public void TearDown() { }

        private IsIdentifiableQueueConsumer GetNewIsIdentifiableQueueConsumer(
            IProducerModel<ExtractedFileVerificationMessage>? mockProducerModel = null,
            IClassifier? mockClassifier = null
        )
        {
            var consumer = new IsIdentifiableQueueConsumer(
                mockProducerModel ?? new TestProducer<ExtractedFileVerificationMessage>(),
                _extractRootDirInfo.FullName,
                mockClassifier ?? new Mock<IClassifier>(MockBehavior.Strict).Object,
                _mockFs
            );
            consumer.SetModel(_mockModel.Object);
            consumer.OnFatal += (_, args) => _fatalArgs = args;
            return consumer;
        }

        #endregion

        #region Tests

        [Test]
        public void Constructor_WhitespaceExtractionRoot_ThrowsException()
        {
            var exc = Assert.Throws<ArgumentException>(static () =>
            {
                _ = new IsIdentifiableQueueConsumer(
                    new TestProducer<ExtractedFileVerificationMessage>(),
                    "   ",
                    new Mock<IClassifier>().Object
                );
            });
            Assert.That(exc?.Message, Is.EqualTo("Argument cannot be null or whitespace (Parameter 'extractionRoot')"));
        }

        [Test]
        public void Constructor_MissingExtractRoot_ThrowsException()
        {
            var mockFs = new MockFileSystem();

            var exc = Assert.Throws<DirectoryNotFoundException>(() =>
            {
                _ = new IsIdentifiableQueueConsumer(
                    new TestProducer<ExtractedFileVerificationMessage>(),
                    "foo",
                    new Mock<IClassifier>().Object,
                    mockFs
                );
            });
            Assert.That(exc?.Message, Is.EqualTo("Could not find the extraction root 'foo' in the filesystem"));
        }

        [Test]
        public void ProcessMessage_HappyPath_NoFailures()
        {
            // Arrange
            _mockProducerModel.Bodies.Clear();

            var mockClassifier = new Mock<IClassifier>(MockBehavior.Strict);
            mockClassifier.Setup(static x => x.Classify(It.IsAny<IFileInfo>())).Returns([]);

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel, mockClassifier.Object);

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            Assert.Multiple(() =>
            {
                // Assert

                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));
            });

            Assert.Multiple(() =>
            {
                Assert.That(_mockProducerModel.TotalSent, Is.EqualTo(1));
                Assert.That(_mockProducerModel.LastMessage?.Status, Is.EqualTo(VerifiedFileStatus.NotIdentifiable));
                Assert.That(_mockProducerModel.LastMessage?.Report, Is.EqualTo("[]"));
            });
        }

        [Test]
        public void ProcessMessage_HappyPath_WithFailures()
        {
            // Arrange
            _mockProducerModel.Bodies.Clear();

            var mockClassifier = new Mock<IClassifier>(MockBehavior.Strict);
            var failure = new Failure([new FailurePart("foo", FailureClassification.Person, 123)]);
            var failures = new List<Failure> { failure };
            mockClassifier.Setup(static x => x.Classify(It.IsAny<IFileInfo>())).Returns(failures);

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel, mockClassifier.Object);

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            Assert.Multiple(() =>
            {
                // Assert

                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));
            });
            Assert.Multiple(() =>
            {
                Assert.That(_mockProducerModel.TotalSent, Is.EqualTo(1));
                Assert.That(_mockProducerModel.LastMessage?.Status, Is.EqualTo(VerifiedFileStatus.IsIdentifiable));
                Assert.That(_mockProducerModel.LastMessage?.Report, Is.EqualTo(JsonConvert.SerializeObject(failures)));
            });
        }

        [Test]
        public void ProcessMessage_ExtractedFileStatusNotAnonymised_CallsFatal()
        {
            // Arrange

            var consumer = GetNewIsIdentifiableQueueConsumer();

            _extractedFileStatusMessage.Status = ExtractedFileStatus.ErrorWontRetry;
            _extractedFileStatusMessage.StatusMessage = "foo";

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            // Assert

            TestTimelineAwaiter.Await(() => _fatalArgs != null, "Expected Fatal to be called");
            Assert.Multiple(() =>
            {
                Assert.That(_fatalArgs?.Message, Is.EqualTo("ProcessMessageImpl threw unhandled exception"));
                Assert.That(_fatalArgs?.Exception?.Message, Is.EqualTo("Received an ExtractedFileStatusMessage message with Status 'ErrorWontRetry' and StatusMessage 'foo'"));
                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void ProcessMessage_MissingFile_SendsErrorWontRetry()
        {
            // Arrange
            _mockProducerModel.Bodies.Clear();
            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel);

            _extractedFileStatusMessage.OutputFilePath = "bar-an.dcm";

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            var outPath = _mockFs.Path.Combine(_extractDir, "bar-an.dcm");
            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));
                Assert.That(_mockProducerModel.TotalSent, Is.EqualTo(1));
                Assert.That(_mockProducerModel.LastMessage?.Status, Is.EqualTo(VerifiedFileStatus.ErrorWontRetry));
                Assert.That(_mockProducerModel.LastMessage?.Report, Is.EqualTo($"Exception while processing ExtractedFileStatusMessage: Could not find file to process '{outPath}'"));
            });
        }

        [Test]
        public void ProcessMessage_ClassifierArithmeticException_SendsErrorWontRetry()
        {
            // Arrange
            _mockProducerModel.Bodies.Clear();

            var mockClassifier = new Mock<IClassifier>(MockBehavior.Strict);
            mockClassifier.Setup(static x => x.Classify(It.IsAny<IFileInfo>())).Throws(new ArithmeticException("divide by zero"));

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel, mockClassifier.Object);

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            Assert.Multiple(() =>
            {
                // Assert

                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));

                Assert.That(_mockProducerModel.TotalSent, Is.EqualTo(1));
                Assert.That(_mockProducerModel.LastMessage?.Status, Is.EqualTo(VerifiedFileStatus.ErrorWontRetry));
                Assert.That(_mockProducerModel.LastMessage?.Report, Does.StartWith("Exception while classifying ExtractedFileStatusMessage:\nSystem.ArithmeticException: divide by zero"));
            });
        }

        #endregion
    }
}
