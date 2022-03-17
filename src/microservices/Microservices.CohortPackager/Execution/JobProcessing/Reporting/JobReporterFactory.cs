using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using System;
using System.IO.Abstractions;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public static class JobReporterFactory
    {
        public static IJobReporter GetReporter([NotNull] string reporterTypeStr,
            [NotNull] IExtractJobStore jobStore,
            [NotNull] IFileSystem fileSystem,
            [NotNull] string extractRoot,
            [CanBeNull] string reportNewLine
        )
        {
            return reporterTypeStr switch
            {
                nameof(FileReporter) => new FileReporter(
                    jobStore,
                    fileSystem,
                    extractRoot,
                    reportNewLine
                ),
                nameof(LoggingReporter) => new LoggingReporter(
                    jobStore,
                    reportNewLine
                ),
                _ => throw new ArgumentException($"No case for type, or invalid type string '{reporterTypeStr}'")
            };
        }
    }
}
