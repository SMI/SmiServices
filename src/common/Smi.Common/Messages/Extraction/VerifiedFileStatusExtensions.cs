namespace Smi.Common.Messages.Extraction
{
    public static class VerifiedFileStatusExtensions
    {
        /// <summary>
        /// Returns whether the status represents an error condition.
        /// 
        /// <b>Note: <see cref="VerifiedFileStatus.IsIdentifiable"/> does not count as an error for this check</b>
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool IsError(this VerifiedFileStatus status) => !(status == VerifiedFileStatus.NotIdentifiable || status == VerifiedFileStatus.IsIdentifiable);
    }
}
