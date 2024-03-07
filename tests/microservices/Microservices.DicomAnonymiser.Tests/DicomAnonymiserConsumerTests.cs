using Microservices.DicomAnonymiser.Anonymisers;
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
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;

namespace Microservices.DicomAnonymiser.Tests
{
    public class DicomAnonymiserConsumerTests
    {
        #region Fixture Methods

        private MockFileSystem _mockFs = null!;
        private IDirectoryInfo _dicomRootDirInfo = null!;
        private IDirectoryInfo _extractRootDirInfo = null!;
        private string _extractDir = null!;
        private string _sourceDcmPathAbs = null!;
        private ExtractFileMessage _extractFileMessage = null!;
        private DicomAnonymiserOptions _options = null!;
        private Mock<IModel> _mockModel = null!;

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

            _dicomRootDirInfo = _mockFs.Directory.CreateDirectory("dicom");
            _extractRootDirInfo = _mockFs.Directory.CreateDirectory("extract");

            var extractDirName = "extractDir";
            _extractDir = _mockFs.Path.Combine(_extractRootDirInfo.FullName, extractDirName);
            _mockFs.Directory.CreateDirectory(_extractDir);

            _sourceDcmPathAbs = _mockFs.Path.Combine(_dicomRootDirInfo.FullName, "foo.dcm");
            _mockFs.File.Create(_sourceDcmPathAbs).Dispose();
            _mockFs.File.SetAttributes(_sourceDcmPathAbs, _mockFs.File.GetAttributes(_sourceDcmPathAbs) | FileAttributes.ReadOnly);

            _extractFileMessage = new ExtractFileMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234",
                ExtractionDirectory = extractDirName,
                DicomFilePath = "foo.dcm",
                OutputPath = "foo-an.dcm",
            };

            _options = new DicomAnonymiserOptions
            {
                RoutingKeySuccess = "yay",
                FailIfSourceWriteable = true,
                RoutingKeyFailure = "nay"
            };

            _mockModel = new Mock<IModel>(MockBehavior.Strict);
            _mockModel.Setup(x => x.IsClosed).Returns(false);
            _mockModel.Setup(x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
            _mockModel.Setup(x => x.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()));
        }

        private DicomAnonymiserConsumer GetNewDicomAnonymiserConsumer(
            IDicomAnonymiser? mockDicomAnonymiser = null,
            IProducerModel? mockProducerModel = null
        )
        {
            var consumer = new DicomAnonymiserConsumer(
                _options,
                _dicomRootDirInfo.FullName,
                _extractRootDirInfo.FullName,
                mockDicomAnonymiser ?? new Mock<IDicomAnonymiser>(MockBehavior.Strict).Object,
                mockProducerModel ?? new Mock<IProducerModel>(MockBehavior.Strict).Object,
                _mockFs
            );
            consumer.SetModel(_mockModel.Object);
            return consumer;
        }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void ProcessMessageImpl_HappyPath()
        {
            // Arrange

            Expression<Func<IDicomAnonymiser, ExtractedFileStatus>> expectedAnonCall =
                x => x.Anonymise(
                    It.Is<ExtractFileMessage>(x => x == _extractFileMessage),
                    It.Is<IFileInfo>(x => x.FullName == _sourceDcmPathAbs),
                    It.Is<IFileInfo>(x => x.FullName == _mockFs.Path.Combine(_extractDir, _extractFileMessage.OutputPath)),
                    out It.Ref<string>.IsAny
                );

            var mockAnonymiser = new Mock<IDicomAnonymiser>(MockBehavior.Strict);
            mockAnonymiser
                .Setup(expectedAnonCall)
                .Returns(ExtractedFileStatus.Anonymised);

            Expression<Func<IProducerModel, IMessageHeader>> expectedSendCall =
                x => x.SendMessage(
                    It.Is<ExtractedFileStatusMessage>(x =>
                        x.Status == ExtractedFileStatus.Anonymised &&
                        x.StatusMessage == null &&
                        x.OutputFilePath == _extractFileMessage.OutputPath
                     ),
                    It.IsAny<IMessageHeader>(),
                    _options.RoutingKeySuccess
                );

            var mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel.Setup(expectedSendCall);

            var consumer = GetNewDicomAnonymiserConsumer(mockAnonymiser.Object, mockProducerModel.Object);

            // Act
            consumer.TestMessage(_extractFileMessage);

            // Assert

            TestTimelineAwaiter.Await(() => consumer.AckCount == 1 && consumer.NackCount == 0);

            mockAnonymiser.Verify(expectedAnonCall, Times.Once);
            mockProducerModel.Verify(expectedSendCall, Times.Once);
        }

        [Test]
        public void ProcessMessageImpl_IsIdentifiableExtraction_ThrowsException()
        {
            // Arrange

            _extractFileMessage.IsIdentifiableExtraction = true;

            var consumer = GetNewDicomAnonymiserConsumer();

            FatalErrorEventArgs? fatalArgs = null;
            consumer.OnFatal += (_, args) => fatalArgs = args;

            // Act

            consumer.TestMessage(_extractFileMessage);

            // Assert

            TestTimelineAwaiter.Await(() => fatalArgs != null, "Expected Fatal to be called");
            Assert.AreEqual("ProcessMessageImpl threw unhandled exception", fatalArgs?.Message);
            Assert.AreEqual("DicomAnonymiserConsumer should not handle identifiable extraction messages", fatalArgs!.Exception!.Message);
            Assert.AreEqual(0, consumer.AckCount);
            Assert.AreEqual(0, consumer.NackCount);
        }

        [Test]
        public void ProcessMessageImpl_SourceFileMissing_AcksWithFailureStatus()
        {
            // Arrange

            _mockFs.File.SetAttributes(_sourceDcmPathAbs, _mockFs.File.GetAttributes(_sourceDcmPathAbs) & ~FileAttributes.ReadOnly);
            _mockFs.File.Delete(_sourceDcmPathAbs);

            Expression<Func<IProducerModel, IMessageHeader>> expectedCall =
                x => x.SendMessage(
                    It.Is<ExtractedFileStatusMessage>(x =>
                        x.Status == ExtractedFileStatus.FileMissing &&
                        x.StatusMessage == $"Could not find file to anonymise: '{_sourceDcmPathAbs}'" &&
                        x.OutputFilePath == null
                     ),
                    It.IsAny<IMessageHeader>(),
                    _options.RoutingKeyFailure
                );

            var mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel.Setup(expectedCall);

            var consumer = GetNewDicomAnonymiserConsumer(null, mockProducerModel.Object);

            // Act

            consumer.TestMessage(_extractFileMessage);

            // Assert

            TestTimelineAwaiter.Await(() => consumer.AckCount == 1 && consumer.NackCount == 0);

            mockProducerModel.Verify(expectedCall, Times.Once);
        }

        [Test]
        public void ProcessMessageImpl_FailIfSourceWriteable_AcksWithFailureStatus()
        {
            // Arrange

            _mockFs.File.SetAttributes(_sourceDcmPathAbs, _mockFs.File.GetAttributes(_sourceDcmPathAbs) & ~FileAttributes.ReadOnly);

            Expression<Func<IProducerModel, IMessageHeader>> expectedCall =
                x => x.SendMessage(
                    It.Is<ExtractedFileStatusMessage>(x =>
                        x.Status == ExtractedFileStatus.ErrorWontRetry &&
                        x.StatusMessage == $"Source file was writeable and FailIfSourceWriteable is set: '{_sourceDcmPathAbs}'" &&
                        x.OutputFilePath == null
                     ),
                    It.IsAny<IMessageHeader>(),
                    _options.RoutingKeyFailure
                );
            var mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel.Setup(expectedCall);

            var consumer = GetNewDicomAnonymiserConsumer(null, mockProducerModel.Object);

            // Act

            consumer.TestMessage(_extractFileMessage);

            // Assert

            TestTimelineAwaiter.Await(() => consumer.AckCount == 1 && consumer.NackCount == 0);

            mockProducerModel.Verify(expectedCall, Times.Once);
        }

        [Test]
        public void ProcessMessageImpl_ExtractionDirMissing_ThrowsException()
        {
            // Arrange

            _mockFs.Directory.Delete(_extractDir);

            var consumer = GetNewDicomAnonymiserConsumer();

            FatalErrorEventArgs? fatalArgs = null;
            consumer.OnFatal += (_, args) => fatalArgs = args;

            // Act

            consumer.TestMessage(_extractFileMessage);

            // Assert

            TestTimelineAwaiter.Await(() => fatalArgs != null, "Expected Fatal to be called");

            Assert.AreEqual("ProcessMessageImpl threw unhandled exception", fatalArgs?.Message);
            Assert.AreEqual($"Expected extraction directory to exist: '{_extractDir}'", fatalArgs!.Exception!.Message);
            Assert.AreEqual(0, consumer.AckCount);
            Assert.AreEqual(0, consumer.NackCount);
        }

        [Test]
        public void ProcessMessageImpl_AnonymisationFailed_AcksWithFailureStatus()
        {
            // Arrange

            var mockAnonymiser = new Mock<IDicomAnonymiser>(MockBehavior.Strict);
            mockAnonymiser
                .Setup(x => x.Anonymise(It.IsAny<IFileInfo>(), It.IsAny<IFileInfo>()))
                .Throws(new Exception("oh no"));

            Expression<Func<IProducerModel, IMessageHeader>> expectedCall =
                x => x.SendMessage(
                    It.Is<ExtractedFileStatusMessage>(x =>
                        x.Status == ExtractedFileStatus.ErrorWontRetry &&
                        x.StatusMessage!.StartsWith($"Error anonymising '{_sourceDcmPathAbs}'. Exception message: IDicomAnonymiser") &&
                        x.OutputFilePath == null
                     ),
                    It.IsAny<IMessageHeader>(),
                    _options.RoutingKeyFailure
                );

            var mockProducerModel = new Mock<IProducerModel>();
            mockProducerModel.Setup(expectedCall);

            var consumer = GetNewDicomAnonymiserConsumer(null, mockProducerModel.Object);

            // Act
            consumer.TestMessage(_extractFileMessage);

            // Assert

            TestTimelineAwaiter.Await(() => consumer.AckCount == 1 && consumer.NackCount == 0);

            mockProducerModel.Verify(expectedCall, Times.Once);
        }

        #endregion
    }
}
