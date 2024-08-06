namespace SmiServices.Common.Messages.Extraction
{
    public enum ExtractedFileStatus
    {
        /// <summary>
        /// Unused placeholder value
        /// </summary>
        None = 0,

        /// <summary>
        /// The file has been anonymised successfully
        /// </summary>
        Anonymised,

        /// <summary>
        /// The file could not be anonymised and will not be retired
        /// </summary>
        ErrorWontRetry,

        /// <summary>
        /// The source file could not be found under the given filesystem root
        /// </summary>
        FileMissing,

        /// <summary>
        /// The source file was successfully copied to the destination
        /// </summary>
        Copied,
    }
}
