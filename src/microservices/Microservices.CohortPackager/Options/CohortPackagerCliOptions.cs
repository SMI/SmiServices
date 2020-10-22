using CommandLine;
using CommandLine.Text;
using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Smi.Common.Options;
using System;
using System.Collections.Generic;


namespace Microservices.CohortPackager.Options
{
    public class CohortPackagerCliOptions : CliOptions
    {
        [Option(
            'r',
            "recreate-report",
            Required = false,
            HelpText = "[Optional] Recreate the report for the specified extraction ID, and exit"
        )]
        public Guid ExtractionId { get; set; }

        [Option(
            'f',
            "format",
            Required = false,
            HelpText = "[Optional] The report format to use when --recreate-report is specified"
        )]
        public ReportFormat ReportFormat { get; set; } = ReportFormat.Combined;

        [Usage]
        [UsedImplicitly]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal scenario - run as a service", new CohortPackagerCliOptions { ExtractionId = Guid.Empty });
                yield return new Example("Recreate a single report", new CohortPackagerCliOptions { ExtractionId = Guid.NewGuid(), ReportFormat = ReportFormat.Split });
            }
        }
    }
}
