
using Smi.Common.Messages;

namespace Applications.DicomDirectoryProcessor.Execution.DirectoryFinders
{
    /// <summary>
    /// Interface for classes which scan a directory for dicom files
    /// </summary>
    public interface IDicomDirectoryFinder
    {
        /// <summary>
        /// Performs the directory scan, sending <see cref="AccessionDirectoryMessage"/>s where it finds dicom files
        /// </summary>
        /// <param name="rootDir">The full path to start the scan at</param>
        void SearchForDicomDirectories(string rootDir);

        /// <summary>
        /// Stops the scan if it is still running. Implementations must ensure they exit promptly when requested.
        /// </summary>
        void Stop();
    }
}
