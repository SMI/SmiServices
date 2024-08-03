namespace Smi.Common.Messages.Extraction
{
    public enum VerifiedFileStatus
    {
        /// <summary>
        /// Unused placeholder value
        /// </summary>
        None = 0,

        /// <summary>
        /// The file has not (yet) been verified
        /// </summary>
        NotVerified,

        /// <summary>
        /// The file was scanned and determined to not be identifiable
        /// </summary>
        NotIdentifiable,

        /// <summary>
        /// The file was scanned and determined to be identifiable
        /// </summary>
        IsIdentifiable,

        /// <summary>
        /// There was an error processing the file. Identifiability could not be determined
        /// </summary>
        ErrorWontRetry,
    }
}
