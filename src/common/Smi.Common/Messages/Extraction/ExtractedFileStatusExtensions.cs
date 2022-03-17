namespace Smi.Common.Messages.Extraction
{
    public static class ExtractedFileStatusExtensions
    {
        public static bool IsSuccess(this ExtractedFileStatus status)
        {
            return
                status == ExtractedFileStatus.Anonymised ||
                status == ExtractedFileStatus.Copied;
        }
    }
}
