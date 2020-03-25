
namespace Smi.Common.Messages.Extraction
{
    // TODO(rkm 2020-03-07) Check what errors CTPAnonymiser can actually spit out here
    public enum ExtractFileStatus
    {
        Unknown = 0,

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
