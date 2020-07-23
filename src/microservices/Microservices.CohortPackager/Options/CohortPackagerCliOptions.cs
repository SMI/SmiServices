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
                yield return new Example("Normal Scenario", new CohortPackagerCliOptions { ExtractionId = Guid.NewGuid() });
            }
        }
    }
}
