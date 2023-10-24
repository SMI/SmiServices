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
            HelpText = "[Optional] Recreate the report for the specified extraction ID, and exit. The extraction root will be set to the current directory."
        )]
        [UsedImplicitly]
        public Guid ExtractionId { get; set; }

        [Option(
            'f',
            "format",
            Required = false,
            Default = ReportFormat.Combined,
            HelpText = "[Optional] The report format to use when --recreate-report is specified. This value (even when the default is used) overrides any value set in the YAML config"
        )]
        [UsedImplicitly]
        public ReportFormat ReportFormat { get; set; }

        [Option(
            'o',
            "output-newline",
            Required = false,
            HelpText = "[Optional] The newline string to use when creating the validation reports. Can be specified to create reports for a different platform. Defaults to Environment.NewLine if not set, and overrides any value in the YAML config."
        )]
        [UsedImplicitly]
        public string? OutputNewLine { get; set; }

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
