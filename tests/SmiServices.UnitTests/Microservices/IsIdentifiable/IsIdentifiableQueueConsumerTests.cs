using IsIdentifiable.Failures;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Messaging;
using SmiServices.Microservices.IsIdentifiable;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;

namespace SmiServices.UnitTests.Microservices.IsIdentifiable
{
    public class IsIdentifiableQueueConsumerTests
    {
        #region Fixture Methods

        private MockFileSystem _mockFs = null!;
        private IDirectoryInfo _extractRootDirInfo = null!;
        private string _extractDir = null!;
        ExtractedFileStatusMessage _extractedFileStatusMessage = null!;
        private Mock<IModel> _mockModel = null!;
        FatalErrorEventArgs? _fatalArgs;
        Mock<IProducerModel> _mockProducerModel = null!;
        Expression<Func<IProducerModel, IMessageHeader>> _expectedSendMessageCall = null!;
        ExtractedFileVerificationMessage _response = null!;

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
            _mockFs = new MockFileSystem();
            _extractRootDirInfo = _mockFs.Directory.CreateDirectory("extract");

            var extractDirName = "extractDir";
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
            _mockModel.Setup(x => x.IsClosed).Returns(false);
            _mockModel.Setup(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
            _mockModel.Setup(x => x.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()));

            _fatalArgs = null;

            _mockProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            _expectedSendMessageCall = x => x.SendMessage(It.IsAny<ExtractedFileVerificationMessage>(), It.IsAny<MessageHeader>(), null);
            _mockProducerModel
                .Setup(_expectedSendMessageCall)
                .Callback<IMessage, IMessageHeader, string>((x, _, _) => _response = (ExtractedFileVerificationMessage)x)
                .Returns(new MessageHeader());
        }

        [TearDown]
        public void TearDown() { }

        private IsIdentifiableQueueConsumer GetNewIsIdentifiableQueueConsumer(
            IProducerModel? mockProducerModel = null,
            IClassifier? mockClassifier = null
        )
        {
            var consumer = new IsIdentifiableQueueConsumer(
                mockProducerModel ?? new Mock<IProducerModel>(MockBehavior.Strict).Object,
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
            var exc = Assert.Throws<ArgumentException>(() =>
            {
                new IsIdentifiableQueueConsumer(
                    new Mock<IProducerModel>().Object,
                    "   ",
                    new Mock<IClassifier>().Object
                );
            });
            Assert.That(exc!.Message, Is.EqualTo("Argument cannot be null or whitespace (Parameter 'extractionRoot')"));
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
            Assert.That(exc!.Message, Is.EqualTo("Could not find the extraction root 'foo' in the filesystem"));
        }

        [Test]
        public void ProcessMessage_HappyPath_NoFailures()
        {
            // Arrange

            var mockClassifier = new Mock<IClassifier>(MockBehavior.Strict);
            mockClassifier.Setup(x => x.Classify(It.IsAny<IFileInfo>())).Returns(new List<Failure>());

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel.Object, mockClassifier.Object);

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            Assert.Multiple(() =>
            {
                // Assert

                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));
            });
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(_response.Status, Is.EqualTo(VerifiedFileStatus.NotIdentifiable));
                Assert.That(_response.Report, Is.EqualTo("[]"));
            });
        }

        [Test]
        public void ProcessMessage_HappyPath_WithFailures()
        {
            // Arrange

            var mockClassifier = new Mock<IClassifier>(MockBehavior.Strict);
            var failure = new Failure(new List<FailurePart> { new FailurePart("foo", FailureClassification.Person, 123) });
            var failures = new List<Failure> { failure };
            mockClassifier.Setup(x => x.Classify(It.IsAny<IFileInfo>())).Returns(failures);

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel.Object, mockClassifier.Object);

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            Assert.Multiple(() =>
            {
                // Assert

                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));
            });
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(_response.Status, Is.EqualTo(VerifiedFileStatus.IsIdentifiable));
                Assert.That(_response.Report, Is.EqualTo(JsonConvert.SerializeObject(failures)));
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
                Assert.That(_fatalArgs!.Exception!.Message, Is.EqualTo("Received an ExtractedFileStatusMessage message with Status 'ErrorWontRetry' and StatusMessage 'foo'"));
                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void ProcessMessage_MissingFile_SendsErrorWontRetry()
        {
            // Arrange

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel.Object);

            _extractedFileStatusMessage.OutputFilePath = "bar-an.dcm";

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            Assert.Multiple(() =>
            {
                // Assert

                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));
            });
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.That(_response.Status, Is.EqualTo(VerifiedFileStatus.ErrorWontRetry));
            var outPath = _mockFs.Path.Combine(_extractDir, "bar-an.dcm");
            Assert.That(_response.Report, Is.EqualTo($"Exception while processing ExtractedFileStatusMessage: Could not find file to process '{outPath}'"));
        }

        [Test]
        public void ProcessMessage_ClassifierArithmeticException_SendsErrorWontRetry()
        {
            // Arrange

            var mockClassifier = new Mock<IClassifier>(MockBehavior.Strict);
            mockClassifier.Setup(x => x.Classify(It.IsAny<IFileInfo>())).Throws(new ArithmeticException("divide by zero"));

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel.Object, mockClassifier.Object);

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            Assert.Multiple(() =>
            {
                // Assert

                Assert.That(consumer.NackCount, Is.EqualTo(0));
                Assert.That(consumer.AckCount, Is.EqualTo(1));
            });
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(_response.Status, Is.EqualTo(VerifiedFileStatus.ErrorWontRetry));
                Assert.That(_response.Report, Does.StartWith("Exception while classifying ExtractedFileStatusMessage:\nSystem.ArithmeticException: divide by zero"));
            });
        }

        #endregion
    }
}
