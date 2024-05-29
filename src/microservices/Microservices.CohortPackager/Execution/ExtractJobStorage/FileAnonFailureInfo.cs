using System;
using System.Diagnostics.CodeAnalysis;

namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Provides information on why a requested file could not be anonymised
    /// </summary>
    public class FileAnonFailureInfo
    {
        /// <summary>
        /// The path of the original DICOM file which could not be extracted
        /// </summary>
        public readonly string DicomFilePath;

        /// <summary>
        /// The reason for the file not being extracted
        /// </summary>
        public readonly string Reason;

        public FileAnonFailureInfo(
            [NotNull] string? dicomFilePath,
            [NotNull] string? reason
        )
        {
            DicomFilePath = string.IsNullOrWhiteSpace(dicomFilePath) ? throw new ArgumentException(nameof(dicomFilePath)) : dicomFilePath;
            Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException(nameof(reason)) : reason;
        }
    }
}
