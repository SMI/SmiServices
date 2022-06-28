using FellowOakDicom;
using Rdmp.Dicom.Extraction.FoDicomBased.DirectoryDecisions;
using System.IO;
using System.IO.Abstractions;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    internal class PutHere : IPutDicomFilesInExtractionDirectories
    {
        private readonly IFileInfo _destination;

        public PutHere(IFileInfo destination)
        {
            this._destination = destination;
        }

        public string WriteOutDataset(DirectoryInfo outputDirectory, string releaseIdentifier, DicomDataset dicomDataset)
        {
            new DicomFile(dicomDataset)
                .Save(_destination.FullName);
            return _destination.FullName;
        }
    }
}