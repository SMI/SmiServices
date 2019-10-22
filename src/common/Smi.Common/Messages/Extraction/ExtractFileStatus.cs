
namespace Smi.Common.Messages.Extraction
{
    public enum ExtractFileStatus
    {
        Unknown = -1,

        /// <summary>
        /// The file has been anonymised successfully
        /// </summary>
        Anonymised,

        /// <summary>
        /// The file could not be anonymised but will be retried later
        /// </summary>
        ErrorWillRetry,

        /// <summary>
        /// The file could not be anonymised and will not be retired
        /// </summary>
        ErrorWontRetry
    }
}
