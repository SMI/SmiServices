
using DicomTypeTranslation;
using FellowOakDicom;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using SmiServices.Microservices.DicomRelationalMapper;
using System.IO;

namespace SmiServices.UnitTests.Microservices.DicomRelationalMapper;

public class AutoRoutingAttacherTests
{

    [Test]
    public void TestPatientAgeTag()
    {
        string filename = Path.Combine(TestContext.CurrentContext.TestDirectory, "test.dcm");

        var dataset = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, "123.123.123" },
            { DicomTag.SOPClassUID, "123.123.123" },
            new DicomAgeString(DicomTag.PatientAge, "009Y")
        };

        var cSharpValue = DicomTypeTranslaterReader.GetCSharpValue(dataset, DicomTag.PatientAge);

        Assert.That(cSharpValue, Is.EqualTo("009Y"));


        var file = new DicomFile(dataset);
        file.Save(filename);


        var source = new DicomFileCollectionSource
        {
            FilenameField = "Path"
        };
        source.PreInitialize(new ExplicitListDicomFileWorklist([filename]), ThrowImmediatelyDataLoadEventListener.Quiet);


        var chunk = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

        Assert.That(chunk.Rows[0]["PatientAge"], Is.EqualTo("009Y"));
    }
}
