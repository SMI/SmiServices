namespace Smi.Common.Messages.Extraction
{
    /// <summary>
    /// Interface for all messages which describe extraction of a single file
    /// </summary>
    public interface IExtractFileMessage : IExtractMessage, IFileReferenceMessage
    {
        /// <summary>
        /// The subdirectory and dicom filename within the ExtractionDirectory to extract the identifiable image
        /// (specified by <see cref="IFileReferenceMessage.DicomFilePath"/>) into. For example "Series132\1234-an.dcm"
        /// </summary>
        public string OutputPath { get; }


        /// <summary>
        /// DICOM tag (0020,000D)
        /// </summary>
        public string StudyInstanceUID { get; }

        /// <summary>
        /// DICOM tag (0020,000E)
        /// </summary>
        public string SeriesInstanceUID { get; }

        /// <summary>
        /// DICOM tag (0008,0018)
        /// </summary>
        public string SOPInstanceUID { get; }


        /// <summary>
        /// Replacement for DICOM tag (0020,000D)
        /// </summary>
        public string ReplacementStudyInstanceUID { get; }

        /// <summary>
        /// Replacement for DICOM tag (0020,000E)
        /// </summary>
        public string ReplacementSeriesInstanceUID { get; }

        /// <summary>
        /// Replacement for DICOM tag (0008,0018)
        /// </summary>
        public string ReplacementSOPInstanceUID { get; }
    }
}
