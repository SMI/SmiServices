using FellowOakDicom;
using Moq;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser;
using SmiServices.Microservices.DicomAnonymiser.Anonymisers;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SmiServices.IntegrationTests.Microservices.DicomAnonymiser;

[RequiresRabbit]
public class DicomAnonymiserHostTests
{
    private DirectoryInfo _tempTestDir = null!;
    private DirectoryInfo _dicomRoot = null!;
    private string _fakeDicom = null!;

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

    [Test]
    public void Integration_HappyPath_MockAnonymiser()
    {
        GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Integration_HappyPath_MockAnonymiser));
        globals.FileSystemOptions!.FileSystemRoot = _dicomRoot.FullName;

        var extractRoot = Directory.CreateDirectory(Path.Combine(_tempTestDir.FullName, "extractRoot"));
        globals.FileSystemOptions.ExtractRoot = extractRoot.FullName;

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
            Modality = "CT",
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
                        It.Is<FileInfo>(x => x.Name == _fakeDicom),
                        It.Is<FileInfo>(x => x.Name == Path.Combine(extractDirAbs.FullName, "foo-an.dcm")),
                        It.IsAny<string>(),
                        out It.Ref<string?>.IsAny
                )
            )
            .Callback(() => File.Create(expectedAnonPathAbs).Dispose())
            .Returns(ExtractedFileStatus.Anonymised);

        var statusExchange = globals.DicomAnonymiserOptions!.ExtractFileStatusProducerOptions!.ExchangeName!;
        var successQueue = globals.IsIdentifiableServiceOptions!.QueueName!;
        var failureQueue = globals.CohortPackagerOptions!.NoVerifyStatusOptions!.QueueName!;

        List<ExtractedFileStatusMessage> statusMessages = [];

            using var tester = new MicroserviceTester(
                globals.RabbitOptions!,
                globals.DicomAnonymiserOptions.AnonFileConsumerOptions!
            );

            tester.CreateExchange(statusExchange, successQueue, isSecondaryBinding: false, routingKey: "verify");
            tester.CreateExchange(statusExchange, failureQueue, isSecondaryBinding: true, routingKey: "noverify");

            tester.SendMessage(globals.DicomAnonymiserOptions.AnonFileConsumerOptions!, new MessageHeader(), testExtractFileMessage);

            var host = new DicomAnonymiserHost(globals, mockAnonymiser.Object);

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
            tester.Dispose();

        var statusMessage = statusMessages.Single();
        Assert.Multiple(() =>
        {
            Assert.That(statusMessage.Status, Is.EqualTo(ExtractedFileStatus.Anonymised), statusMessage.StatusMessage);
            Assert.That(File.Exists(expectedAnonPathAbs), Is.True);
        });
    }
}
