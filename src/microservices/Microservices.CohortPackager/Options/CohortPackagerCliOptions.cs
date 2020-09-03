using CommandLine;
using CommandLine.Text;
using JetBrains.Annotations;
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
            HelpText = "[Optional] Recreate the report for the specified extraction ID"
        )]
        public Guid ExtractionId { get; set; }

        [Usage]
        [UsedImplicitly]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal scenario - run as a service", new CohortPackagerCliOptions { ExtractionId = Guid.Empty });
                yield return new Example("Recreate a single report", new CohortPackagerCliOptions { ExtractionId = Guid.NewGuid() });
            }
        }
    }
}
