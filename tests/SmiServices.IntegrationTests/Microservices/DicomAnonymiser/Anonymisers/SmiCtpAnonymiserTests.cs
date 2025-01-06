using FellowOakDicom;
using NUnit.Framework;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.DicomAnonymiser.Anonymisers;
using SmiServices.UnitTests.TestCommon;
using System.IO;

namespace SmiServices.IntegrationTests.Microservices.DicomAnonymiser.Anonymisers;

internal class SmiCtpAnonymiserTests
{
    [Test]
    public void Anonymise_HappyPath_IsOk()
    {
        // Arrange

        var globals = new GlobalOptionsFactory().Load(nameof(Anonymise_HappyPath_IsOk));
        globals.DicomAnonymiserOptions!.CtpAnonCliJar = FixtureSetup.CtpJarPath;
        globals.DicomAnonymiserOptions.CtpAllowlistScript = FixtureSetup.CtpAllowlistPath;
        globals.DicomAnonymiserOptions.SRAnonymiserToolPath = null;

        var ds = new DicomDataset
        {
            { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
            { DicomTag.StudyInstanceUID, "1" },
            { DicomTag.SeriesInstanceUID, "2" },
            { DicomTag.SOPInstanceUID, "3" },
        };
        var srcDcm = new DicomFile(ds);

        using var tempDir = new DisposableTempDir();
        var srcPath = Path.Combine(tempDir, "in.dcm");
        srcDcm.Save(srcPath);
        File.SetAttributes(srcPath, FileAttributes.ReadOnly);
        var srcFile = new FileInfo(srcPath);
        var destPath = Path.Combine(tempDir, "out.dcm");
        var destFile = new FileInfo(destPath);

        using var anonymiser = new SmiCtpAnonymiser(globals);

        // Act

        var status = anonymiser.Anonymise(srcFile, destFile, "CT", out var message);

        // Assert

        Assert.Multiple(() =>
        {

            Assert.That(status, Is.EqualTo(ExtractedFileStatus.Anonymised));
            Assert.That(message, Is.Null);
        });

        File.SetAttributes(srcPath, FileAttributes.Normal);
    }
}
