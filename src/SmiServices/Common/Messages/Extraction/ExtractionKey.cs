namespace SmiServices.Common.Messages.Extraction
{
    // ReSharper disable InconsistentNaming
    public enum ExtractionKey
    {
        /// <summary>
        /// Dicom Tag (0008,0018)
        /// </summary>
        SOPInstanceUID,

        /// <summary>
        /// Dicom Tag (0020,000E) 
        /// </summary>
        SeriesInstanceUID,

        /// <summary>
        ///  Dicom Tag (0020,000D)
        /// </summary>
        StudyInstanceUID,
    }
}
