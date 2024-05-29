using System;
using System.Diagnostics.CodeAnalysis;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage
{
    /// <summary>
    /// Contains information for an anonymised file which failed the validation checks
    /// </summary>
    public class FileVerificationFailureInfo
    {
        /// <summary>
        /// The anonymised file path which has failed, relative to the extraction directory
        /// </summary>
        public readonly string AnonFilePath;

        // NOTE(rkm 2020-10-28) This is a JSON string for now, but might be worth deserializing it into a Failure object here (instead of in JobReporter)
        /// <summary>
        /// The failure data from the validation checks, as a JSON string
        /// </summary>
        public readonly string Data;


        public FileVerificationFailureInfo(
            [NotNull] string? anonFilePath,
            [NotNull] string? failureData
        )
        {
            AnonFilePath = string.IsNullOrWhiteSpace(anonFilePath) ? throw new ArgumentException(nameof(anonFilePath)) : anonFilePath;
            Data = string.IsNullOrWhiteSpace(failureData) ? throw new ArgumentException(nameof(failureData)) : failureData;
        }
    }
}
