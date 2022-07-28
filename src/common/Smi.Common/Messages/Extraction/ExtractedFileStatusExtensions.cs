namespace Smi.Common.Messages.Extraction
{
    public static class ExtractedFileStatusExtensions
    {
        /// <summary>
        /// Returns whether the status represents an error condition
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsError(this ExtractedFileStatus status) => !(status == ExtractedFileStatus.Anonymised || status == ExtractedFileStatus.Copied);
    }
}
