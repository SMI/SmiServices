using FellowOakDicom;
using System.IO.Abstractions;

namespace SmiServices.UnitTests.Common
{
    public static class DicomFileTestHelpers
    {
        public static DicomDataset DefaultDicomDataset() => new()
        {
            { DicomTag.SOPClassUID, DicomUID.MRImageStorage },
            { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
            { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
            { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
            { DicomTag.Modality, "MR" },
            { DicomTag.PatientID, "PatientID" },
        };

        /// <summary>
        /// Writes a DICOM file to the specified <paramref name="fileInfo"/> using the
        /// <see cref="DefaultDicomDataset"/> unless one is passed in
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="ds"></param>
        public static void WriteSampleDicomFile(IFileInfo fileInfo, DicomDataset? ds = null)
        {
            ds ??= DefaultDicomDataset();
            using var stream = fileInfo.OpenWrite();
            new DicomFile(ds).Save(stream);
        }
    }
}
