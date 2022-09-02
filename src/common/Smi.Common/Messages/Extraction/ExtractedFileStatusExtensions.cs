namespace Smi.Common.Messages.Extraction
{
    public static class ExtractedFileStatusExtensions
    {
        public static bool ExtractionSucceeded(this ExtractedFileStatus status) => status == ExtractedFileStatus.Anonymised || status == ExtractedFileStatus.Copied;
    }
}