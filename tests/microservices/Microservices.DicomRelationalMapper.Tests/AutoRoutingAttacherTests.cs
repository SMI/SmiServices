
using FellowOakDicom;
using DicomTypeTranslation;
using Microservices.DicomRelationalMapper.Execution;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using System.IO;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Microservices.Tests.RDMPTests
{
    public class AutoRoutingAttacherTests
    {

        [Test]
        public void TestPatientAgeTag()
        {
            string filename = Path.Combine(TestContext.CurrentContext.TestDirectory, "test.dcm");

            var dataset = new DicomDataset();
            dataset.Add(DicomTag.SOPInstanceUID, "123.123.123");
            dataset.Add(DicomTag.SOPClassUID, "123.123.123");
            dataset.Add(new DicomAgeString(DicomTag.PatientAge, "009Y"));

            var cSharpValue = DicomTypeTranslaterReader.GetCSharpValue(dataset, DicomTag.PatientAge);

            Assert.That(cSharpValue, Is.EqualTo("009Y"));


            var file = new DicomFile(dataset);
            file.Save(filename);


            var source = new DicomFileCollectionSource();
            source.FilenameField = "Path";
            source.PreInitialize(new ExplicitListDicomFileWorklist(new[] { filename }), ThrowImmediatelyDataLoadEventListener.Quiet);


            var chunk = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

            Assert.That(chunk.Rows[0]["PatientAge"], Is.EqualTo("009Y"));
        }
    }
}
