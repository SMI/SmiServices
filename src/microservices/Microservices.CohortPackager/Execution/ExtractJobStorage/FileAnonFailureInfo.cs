using JetBrains.Annotations;
using Smi.Common.Messages.Extraction;
using System;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Provides information on why a requested file could not be anonymised
    /// </summary>
    public class FileAnonFailureInfo
    {
        /// <summary>
        /// The source DICOM file, relative to the filesystem root
        /// </summary>
        [NotNull] public readonly string DicomFilePath;

        /// <summary>
        /// TODO
        /// </summary>
        [NotNull] public readonly ExtractedFileStatus Status;

        /// <summary>
        /// The reason for the file not being extracted
        /// </summary>
        [NotNull] public readonly string StatusMessage;

        public FileAnonFailureInfo(
            [NotNull] string dicomFilePath,
            ExtractedFileStatus status,
            [NotNull] string statusMessage
        )
        {
            DicomFilePath = string.IsNullOrWhiteSpace(dicomFilePath) ? throw new ArgumentException(null, nameof(dicomFilePath)) : dicomFilePath;
            Status = (status == default || status.ExtractionSucceeded()) ? throw new ArgumentException("Invalid status (default or successful)", nameof(status)) : status;
            StatusMessage = string.IsNullOrWhiteSpace(statusMessage) ? throw new ArgumentException(null, nameof(statusMessage)) : statusMessage;
        }
    }
}
