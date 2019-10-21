
namespace Microservices.CohortExtractor.Execution
{
    public enum ExtractionKey
    {
        /// <summary>
        /// Unknown tag
        /// </summary>
        Unknown,

        /// <summary> 
        /// Dicom Tag (0008,0018)
        /// </summary>
        SOPInstanceUID,

        /// <summary>
        /// Dicom Tag (0020,000E)
        /// </summary>
        SeriesInstanceUID
    }
}
