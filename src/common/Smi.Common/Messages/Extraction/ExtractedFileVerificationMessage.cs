namespace Smi.Common.Messages.Extraction
{
    public class ExtractedFileVerificationMessage : ExtractMessage, IFileReferenceMessage
    {
        public bool IsIdentifiable { get; set; }

        public string Report { get; set; }

        /// <summary>
        /// The originally sourced origin (identifiable file path).
        /// </summary>
        public string DicomFilePath { get; set; }

        /// <summary>
        /// Output file path, relative to the extraction directory. Only required if an output file has been produced
        /// </summary>
        public string OutputFilePath { get; set; }

        public ExtractedFileVerificationMessage() { }

        public ExtractedFileVerificationMessage(ExtractedFileStatusMessage request)
            : base(request)
        {
            DicomFilePath = request.DicomFilePath;
            OutputFilePath = request.OutputFilePath;
        }
    }
}
