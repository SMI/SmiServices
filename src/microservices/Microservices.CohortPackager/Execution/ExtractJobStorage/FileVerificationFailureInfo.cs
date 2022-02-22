using JetBrains.Annotations;
using System;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Contains information for an anonymised file which failed the validation checks
    /// </summary>
    public class FileVerificationFailureInfo
    {
        /// <summary>
        /// The file path containing the failure, relative to the extraction directory
        /// </summary>
        [NotNull] public readonly string RelativeOutputFilePath;

        // NOTE(rkm 2020-10-28) This is a JSON string for now, but might be worth deserializing it into a Failure object here (instead of in JobReporterBase)
        /// <summary>
        /// The failure data from the validation checks, as a JSON string
        /// </summary>
        [NotNull] public readonly string Data;


        public FileVerificationFailureInfo(
            [NotNull] string relativeOutputFilePath,
            [NotNull] string failureData
        )
        {
            RelativeOutputFilePath = string.IsNullOrWhiteSpace(relativeOutputFilePath) ? throw new ArgumentException(nameof(relativeOutputFilePath)) : relativeOutputFilePath;
            Data = string.IsNullOrWhiteSpace(failureData) ? throw new ArgumentException(nameof(failureData)) : failureData;
        }
    }
}