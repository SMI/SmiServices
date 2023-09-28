using IsIdentifiable.Failures;
using IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Service;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;

namespace Microservices.IsIdentifiable.Tests.Service
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
            _expectedSendMessageCall = x => x.SendMessage(It.IsAny<ExtractedFileVerificationMessage>(), null, null);
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
            Assert.AreEqual("Argument cannot be null or whitespace (Parameter 'extractionRoot')", exc!.Message);
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
            Assert.AreEqual("Could not find the extraction root 'foo' in the filesystem", exc!.Message);
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

            // Assert

            Assert.AreEqual(0, consumer.NackCount);
            Assert.AreEqual(1, consumer.AckCount);
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.AreEqual(VerifiedFileStatus.NotIdentifiable, _response.Status);
            Assert.AreEqual("[]", _response.Report);
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

            // Assert

            Assert.AreEqual(0, consumer.NackCount);
            Assert.AreEqual(1, consumer.AckCount);
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.AreEqual(VerifiedFileStatus.IsIdentifiable, _response.Status);
            Assert.AreEqual(JsonConvert.SerializeObject(failures), _response.Report);
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
            Assert.AreEqual("ProcessMessageImpl threw unhandled exception", _fatalArgs?.Message);
            Assert.AreEqual("Received an ExtractedFileStatusMessage message with Status 'ErrorWontRetry' and StatusMessage 'foo'", _fatalArgs!.Exception!.Message);
            Assert.AreEqual(0, consumer.NackCount);
            Assert.AreEqual(0, consumer.AckCount);
        }

        [Test]
        public void ProcessMessage_MissingFile_SendsErrorWontRetry()
        {
            // Arrange

            var consumer = GetNewIsIdentifiableQueueConsumer(_mockProducerModel.Object);

            _extractedFileStatusMessage.OutputFilePath = "bar-an.dcm";

            // Act

            consumer.TestMessage(_extractedFileStatusMessage);

            // Assert

            Assert.AreEqual(0, consumer.NackCount);
            Assert.AreEqual(1, consumer.AckCount);
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.AreEqual(VerifiedFileStatus.ErrorWontRetry, _response.Status);
            var outPath = _mockFs.Path.Combine(_extractDir, "bar-an.dcm");
            Assert.AreEqual($"Exception while processing ExtractedFileStatusMessage: Could not find file to process '{outPath}'", _response.Report);
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

            // Assert

            Assert.AreEqual(0, consumer.NackCount);
            Assert.AreEqual(1, consumer.AckCount);
            _mockProducerModel.Verify(_expectedSendMessageCall, Times.Once);
            Assert.AreEqual(VerifiedFileStatus.ErrorWontRetry, _response.Status);
            Assert.True(_response.Report.StartsWith("Exception while classifying ExtractedFileStatusMessage:\nSystem.ArithmeticException: divide by zero"));
        }

        #endregion
    }
}
