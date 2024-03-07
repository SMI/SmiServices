using Microservices.CohortPackager.Execution.ExtractJobStorage;
using System;
using System.IO.Abstractions;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting
{
    public static class JobReporterFactory
    {
        public static IJobReporter GetReporter(string reporterTypeStr,
            IExtractJobStore jobStore,
            IFileSystem fileSystem,
            string extractRoot,
            string reportFormatStr,
            string? reportNewLine
        )
        {
            if (!Enum.TryParse(reportFormatStr, ignoreCase: true, out ReportFormat reportFormat))
                throw new ArgumentException($"Could not parse reportFormatStr to a valid ReportFormat. Got '{reportFormatStr}'");

            return reporterTypeStr switch
            {
                nameof(FileReporter) => new FileReporter(
                    jobStore,
                    fileSystem,
                    extractRoot,
                    reportFormat,
                    reportNewLine
                ),
                nameof(LoggingReporter) => new LoggingReporter(
                    jobStore,
                    reportFormat,
                    reportNewLine
                ),
                _ => throw new ArgumentException($"No case for type, or invalid type string '{reporterTypeStr}'")
            };
        }
    }
}
