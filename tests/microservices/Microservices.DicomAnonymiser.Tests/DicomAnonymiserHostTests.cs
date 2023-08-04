using Microservices.DicomAnonymiser.Anonymisers;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;

namespace Microservices.DicomAnonymiser.Tests
{
    [RequiresRabbit]
    public class DicomAnonymiserHostTests
    {
        #region Fixture Methods

        private DirectoryInfo _tempTestDir = null!;
        private DirectoryInfo _dicomRoot = null!;
        private string _fakeDicom = null!;

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
            // TODO(rkm 2022-12-08) check this is set properly for each test
            var tempTestDirPath = TestFileSystemHelpers.GetTemporaryTestDirectory();
            _tempTestDir = Directory.CreateDirectory(tempTestDirPath);
            _dicomRoot = Directory.CreateDirectory(Path.Combine(_tempTestDir.FullName, "dicom"));
            _fakeDicom = Path.Combine(_dicomRoot.FullName, "foo.dcm");
        }

        [TearDown]
        public void TearDown()
        {
            File.SetAttributes(_fakeDicom, FileAttributes.Normal);
            _tempTestDir.Delete(recursive: true);
        }

        #endregion

        #region Tests5

        [Test]
        public void Integration_HappyPath_MockAnonymiser()
        {
            // Arrange

            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Integration_HappyPath_MockAnonymiser));
            globals.FileSystemOptions.FileSystemRoot = _dicomRoot.FullName;

            var extractRoot = Directory.CreateDirectory(Path.Combine(_tempTestDir.FullName, "extractRoot"));
            globals.FileSystemOptions.ExtractRoot = extractRoot.FullName;

            File.Create(_fakeDicom).Dispose();
            File.SetAttributes(_fakeDicom, File.GetAttributes(_fakeDicom) | FileAttributes.ReadOnly);

            var extractDirAbs = Directory.CreateDirectory(Path.Combine(extractRoot.FullName, "extractDir"));
            var expectedAnonPathAbs = Path.Combine(extractDirAbs.FullName, "foo-an.dcm");

            var testExtractFileMessage = new ExtractFileMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234",
                ExtractionDirectory = extractDirAbs.Name,
                JobSubmittedAt = DateTime.UtcNow,
                IsIdentifiableExtraction = false,
                IsNoFilterExtraction = false,

                DicomFilePath = "foo.dcm",
                OutputPath = "foo-an.dcm",
            };

            var mockAnonymiser = new Mock<IDicomAnonymiser>(MockBehavior.Strict);
            mockAnonymiser
                .Setup(
                    x => x.Anonymise(
                        It.Is<IFileInfo>(x => x.FullName == _fakeDicom),
                        It.Is<IFileInfo>(x => x.FullName == Path.Combine(extractDirAbs.FullName, "foo-an.dcm"))
                    )
                )
                .Callback(() => File.Create(expectedAnonPathAbs).Dispose())
                .Returns(ExtractedFileStatus.Anonymised);

            var statusExchange = globals.DicomAnonymiserOptions.ExtractFileStatusProducerOptions.ExchangeName;
            var successQueue = globals.IsIdentifiableServiceOptions.QueueName;
            var failureQueue = globals.CohortPackagerOptions.NoVerifyStatusOptions.QueueName;

            List<ExtractedFileStatusMessage> statusMessages = new();

            using (
                var tester = new MicroserviceTester(
                    globals.RabbitOptions,
                    globals.DicomAnonymiserOptions.AnonFileConsumerOptions
                )
            )
            {
                tester.CreateExchange(statusExchange, successQueue, isSecondaryBinding: false, routingKey: "verify");
                tester.CreateExchange(statusExchange, failureQueue, isSecondaryBinding: true, routingKey: "noverify");

                tester.SendMessage(globals.DicomAnonymiserOptions.AnonFileConsumerOptions, new MessageHeader(), testExtractFileMessage);

                var host = new DicomAnonymiserHost(globals, mockAnonymiser.Object);

                // Act

                host.Start();

                var timeoutSecs = 10;

                while (statusMessages.Count == 0 && timeoutSecs > 0)
                {
                    statusMessages.AddRange(tester.ConsumeMessages<ExtractedFileStatusMessage>(successQueue).Select(x => x.Item2));
                    statusMessages.AddRange(tester.ConsumeMessages<ExtractedFileStatusMessage>(failureQueue).Select(x => x.Item2));

                    --timeoutSecs;
                    if (statusMessages.Count == 0)
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                host.Stop("Test end");
            }

            // Assert

            var statusMessage = statusMessages.Single();
            Assert.AreEqual(ExtractedFileStatus.Anonymised, statusMessage.Status, statusMessage.StatusMessage);
            Assert.True(File.Exists(expectedAnonPathAbs));
        }

        #endregion
    }
}
