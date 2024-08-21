using FellowOakDicom;
using System.IO.Abstractions;

namespace SmiServices.UnitTests.Common
{
    public static class DicomFileTestHelpers
    {
        /// <summary>
        /// Writes a DICOM file to the specified <paramref name="fileInfo"/> using a sample
        /// <paramref name="ds"/> unless one is passed in
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="ds"></param>
        public static void WriteSampleDicomFile(IFileInfo fileInfo, DicomDataset? ds = null)
        {
            ds ??= new DicomDataset
            {
                { DicomTag.SOPClassUID, DicomUID.CTImageStorage },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
            };
            using var stream = fileInfo.OpenWrite();
            new DicomFile(ds).Save(stream);
        }
    }
}
