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
using FellowOakDicom;

namespace Microservices.DicomAnonymiser.Tests
{
    [RequiresRabbit]
    public class DicomAnonymiserHostTests
    {
        #region Fixture Methods

        // Private fields (_tempTestDir, _dicomRoot, _fakeDicom) that 
        // are used in the setup and teardown of each test.
        private DirectoryInfo _tempTestDir = null!;
        private DirectoryInfo _dicomRoot = null!;
        private string _fakeDicom = null!;


        // [OneTimeSetUp] and [OneTimeTearDown] methods are run once 
        // before and after all the tests in the class, respectively. 
        // In this case, the setup method is used to set up a logger.
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { } 

        #endregion

        #region Test Methods

        // [SetUp] and [TearDown] methods are run before and after each 
        // test, respectively. In this case, the setup method creates a 
        // temporary directory and a fake DICOM file, and the teardown 
        // method deletes the temporary directory.
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

        #region Tests

        // The Integration_HappyPath_MockAnonymiser method is a test case
        // structured in the Arrange-Act-Assert pattern. It tests for the 
        // scenario where the DICOM Anonymiser successfully anonymises a 
        // DICOM file.
        [Test]
        public void Integration_HappyPath_MockAnonymiser()
        {
            // Arrange
            // It sets up the necessary objects and state for the test. This
            // includes creating a mock DICOM Anonymiser, setting file paths,
            // and creating a DicomAnonymiserHost.

            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Integration_HappyPath_MockAnonymiser));
            globals.FileSystemOptions!.FileSystemRoot = _dicomRoot.FullName;

            var extractRoot = Directory.CreateDirectory(Path.Combine(_tempTestDir.FullName, "extractRoot"));
            globals.FileSystemOptions.ExtractRoot = extractRoot.FullName;

            // NOTE: The commented out code below is an alternative way to create
            // a fake DICOM file, however, it is not used in this test.
            // File.Create(_fakeDicom).Dispose();
            // File.SetAttributes(_fakeDicom, File.GetAttributes(_fakeDicom) | FileAttributes.ReadOnly);

            var dicomFile = new DicomFile();
            dicomFile.Dataset.Add(DicomTag.PatientID, "12345678");
            dicomFile.Dataset.Add(DicomTag.Modality, "CT");
            dicomFile.Dataset.Add(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dicomFile.Dataset.Add(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUID.SecondaryCaptureImageStorage;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.ImplementationClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.Save(_fakeDicom);

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

            // The test uses the Moq library to create a mock implementation of 
            // the IDicomAnonymiser interface. This allows the test to control the
            // behavior of the DICOM Anonymiser and verify that it is called with
            // the correct arguments.
            var mockAnonymiser = new Mock<IDicomAnonymiser>(MockBehavior.Strict);
            mockAnonymiser
                .Setup(
                    x => x.Anonymise(
                        It.Is<ExtractFileMessage>(x => x.ExtractionJobIdentifier == testExtractFileMessage.ExtractionJobIdentifier),
                        It.Is<IFileInfo>(x => x.FullName == _fakeDicom),
                        It.Is<IFileInfo>(x => x.FullName == Path.Combine(extractDirAbs.FullName, "foo-an.dcm")),
                        out It.Ref<string>.IsAny
                    )
                )
                .Callback(() => File.Create(expectedAnonPathAbs).Dispose())
                .Returns(ExtractedFileStatus.Anonymised);

            var statusExchange = globals.DicomAnonymiserOptions!.ExtractFileStatusProducerOptions!.ExchangeName!;
            var successQueue = globals.IsIdentifiableServiceOptions!.QueueName!;
            var failureQueue = globals.CohortPackagerOptions!.NoVerifyStatusOptions!.QueueName!;

            List<ExtractedFileStatusMessage> statusMessages = new();

            using (
                var tester = new MicroserviceTester(
                    globals.RabbitOptions!,
                    globals.DicomAnonymiserOptions.AnonFileConsumerOptions!
                )
            )
            {
                tester.CreateExchange(statusExchange, successQueue, isSecondaryBinding: false, routingKey: "verify");
                tester.CreateExchange(statusExchange, failureQueue, isSecondaryBinding: true, routingKey: "noverify");

                tester.SendMessage(globals.DicomAnonymiserOptions.AnonFileConsumerOptions!, new MessageHeader(), testExtractFileMessage);

                var host = new DicomAnonymiserHost(globals, mockAnonymiser.Object);

                // Act
                // It starts the DicomAnonymiserHost and waits for it to process 
                //a message.

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
            // It checks that the expected outcome has occurred. In this case, it 
            // checks that the status message indicates that the file was anonymised
            // and that the anonymised file exists.

            var statusMessage = statusMessages.Single();
            Assert.AreEqual(ExtractedFileStatus.Anonymised, statusMessage.Status, statusMessage.StatusMessage);
            Assert.True(File.Exists(expectedAnonPathAbs));
        }

        #endregion
    }
}
