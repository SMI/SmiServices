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
    }
}
