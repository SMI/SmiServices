using System;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Provides information on why a requested file could not be anonymised
    /// </summary>
    public class FileAnonFailureInfo
    {
        // TODO(rkm 2020-10-27) This should probably instead reference the source file which failed to anonymise
        /// <summary>
        /// The path of the output DICOM file which could not be extracted
        /// </summary>
        public readonly string ExpectedAnonFile;

        /// <summary>
        /// The reason for the file not being extracted
        /// </summary>
        public readonly string Reason;

        public FileAnonFailureInfo(
            string? expectedAnonFile,
            string? reason
        )
        {
            ExpectedAnonFile = string.IsNullOrWhiteSpace(expectedAnonFile) ? throw new ArgumentException(null, nameof(expectedAnonFile)) : expectedAnonFile;
            Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException(null, nameof(reason)) : reason;
        }
    }
}
