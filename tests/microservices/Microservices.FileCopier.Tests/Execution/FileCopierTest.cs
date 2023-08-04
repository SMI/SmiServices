using Microservices.FileCopier.Execution;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.IO.Abstractions.TestingHelpers;

namespace Microservices.FileCopier.Tests.Execution
{
    public class FileCopierTest
    {
        private FileCopierOptions _options = null!;

        private MockFileSystem _mockFileSystem = null!;
        private const string FileSystemRoot = "PACS";
        private const string ExtractRoot = "extract";
        private string _relativeSrc = null!;
        private readonly byte[] _expectedContents = { 0b00, 0b01, 0b10, 0b11 };
        private ExtractFileMessage _requestMessage = null!;

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _options = new FileCopierOptions
            {
                NoVerifyRoutingKey = "noverify",
            };

            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.Directory.CreateDirectory(FileSystemRoot);
            _mockFileSystem.Directory.CreateDirectory(ExtractRoot);
            _relativeSrc = _mockFileSystem.Path.Combine("input", "a.dcm");
            string src = _mockFileSystem.Path.Combine(FileSystemRoot, _relativeSrc);
            _mockFileSystem.Directory.CreateDirectory(_mockFileSystem.Directory.GetParent(src)!.FullName);
            _mockFileSystem.File.WriteAllBytes(src, _expectedContents);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp()
        {
            _requestMessage = new ExtractFileMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "123",
                ExtractionDirectory = "proj1",
                DicomFilePath = _relativeSrc,
                OutputPath = "out.dcm",
            };
        }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void Test_FileCopier_HappyPath()
        {
            var mockProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            ExtractedFileStatusMessage? sentStatusMessage = null;
            string? sentRoutingKey = null;
            mockProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), It.IsAny<string>()))
                .Callback((IMessage message, IMessageHeader header, string routingKey) =>
                {
                    sentStatusMessage = (ExtractedFileStatusMessage)message;
                    sentRoutingKey = routingKey;
                })
                .Returns(() => null);

            var requestHeader = new MessageHeader();

            var copier = new ExtractionFileCopier(_options, mockProducerModel.Object, FileSystemRoot, ExtractRoot, _mockFileSystem);
            copier.ProcessMessage(_requestMessage, requestHeader);

            var expectedStatusMessage = new ExtractedFileStatusMessage(_requestMessage)
            {
                DicomFilePath = _requestMessage.DicomFilePath,
                Status = ExtractedFileStatus.Copied,
                OutputFilePath = _requestMessage.OutputPath,
            };
            Assert.AreEqual(expectedStatusMessage, sentStatusMessage);
            Assert.AreEqual(_options.NoVerifyRoutingKey, sentRoutingKey);

            string expectedDest = _mockFileSystem.Path.Combine(ExtractRoot, _requestMessage.ExtractionDirectory, "out.dcm");
            Assert.True(_mockFileSystem.File.Exists(expectedDest));
            Assert.AreEqual(_expectedContents, _mockFileSystem.File.ReadAllBytes(expectedDest));
        }

        [Test]
        public void Test_FileCopier_MissingFile_SendsMessage()
        {
            var mockProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            ExtractedFileStatusMessage? sentStatusMessage = null;
            string? sentRoutingKey = null;
            mockProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), It.IsAny<string>()))
                .Callback((IMessage message, IMessageHeader header, string routingKey) =>
                {
                    sentStatusMessage = (ExtractedFileStatusMessage)message;
                    sentRoutingKey = routingKey;
                })
                .Returns(() => null);

            _requestMessage.DicomFilePath = "missing.dcm";
            var requestHeader = new MessageHeader();

            var copier = new ExtractionFileCopier(_options, mockProducerModel.Object, FileSystemRoot, ExtractRoot, _mockFileSystem);
            copier.ProcessMessage(_requestMessage, requestHeader);

            var expectedStatusMessage = new ExtractedFileStatusMessage(_requestMessage)
            {
                DicomFilePath = _requestMessage.DicomFilePath,
                Status = ExtractedFileStatus.FileMissing,
                OutputFilePath = null,
                StatusMessage = $"Could not find '{_mockFileSystem.Path.Combine(FileSystemRoot, "missing.dcm")}'"
            };
            Assert.AreEqual(expectedStatusMessage, sentStatusMessage);
            Assert.AreEqual(_options.NoVerifyRoutingKey, sentRoutingKey);
        }

        [Test]
        public void Test_FileCopier_ExistingOutputFile_IsOverwritten()
        {
            var mockProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
            ExtractedFileStatusMessage? sentStatusMessage = null;
            string? sentRoutingKey = null;
            mockProducerModel
                .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), It.IsAny<string>()))
                .Callback((IMessage message, IMessageHeader header, string routingKey) =>
                {
                    sentStatusMessage = (ExtractedFileStatusMessage)message;
                    sentRoutingKey = routingKey;
                })
                .Returns(() => null);

            var requestHeader = new MessageHeader();
            string expectedDest = _mockFileSystem.Path.Combine(ExtractRoot, _requestMessage.ExtractionDirectory, "out.dcm");
            _mockFileSystem.Directory.GetParent(expectedDest)!.Create();
            _mockFileSystem.File.WriteAllBytes(expectedDest, new byte[] { 0b0 });

            var copier = new ExtractionFileCopier(_options, mockProducerModel.Object, FileSystemRoot, ExtractRoot, _mockFileSystem);
            copier.ProcessMessage(_requestMessage, requestHeader);

            var expectedStatusMessage = new ExtractedFileStatusMessage(_requestMessage)
            {
                DicomFilePath = _requestMessage.DicomFilePath,
                Status = ExtractedFileStatus.Copied,
                OutputFilePath = _requestMessage.OutputPath,
                StatusMessage = null,
            };
            Assert.AreEqual(expectedStatusMessage, sentStatusMessage);
            Assert.AreEqual(_options.NoVerifyRoutingKey, sentRoutingKey);
            Assert.AreEqual(_expectedContents, _mockFileSystem.File.ReadAllBytes(expectedDest));
        }

        #endregion
    }
}
