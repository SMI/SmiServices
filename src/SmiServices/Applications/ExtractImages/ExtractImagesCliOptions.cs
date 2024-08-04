using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommandLine;
using CommandLine.Text;
using SmiServices.Common.Options;


namespace SmiServices.Applications.ExtractImages
{
    public class ExtractImagesCliOptions : CliOptions
    {
        // Required

        [Option(shortName: 'p', longName: "project-id", Required = true, HelpText = "The project identifier")]
        public string ProjectId { get; set; } = null!;

        [Option(shortName: 'c', longName: "cohort-csv-file", Required = true,
            HelpText = "The CSV file containing IDs of the cohort for extraction")]
        public string CohortCsvFile { get; set; } = null!;

        // Optional

        [Option(shortName: 'm', longName: "modalities", Required = false,
            HelpText =
                "[Optional] List of modalities to extract. Any non-matching IDs from the input list are ignored")]
        public string? Modalities { get; set; }

        [Option(shortName: 'i', longName: "identifiable-extraction", Required = false,
            HelpText = "Extract without performing anonymisation")]
        public bool IsIdentifiableExtraction { get; set; }

        [Option(shortName: 'f', longName: "no-filters", Required = false,
            HelpText = "Extract without applying any rejection filters")]
        public bool IsNoFiltersExtraction { get; set; }

        [Option(shortName: 'n', longName: "non-interactive", Required = false,
            HelpText = "Don't pause for manual confirmation before sending messages")]
        public bool NonInteractive { get; set; }


        [Usage]
        [ExcludeFromCodeCoverage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example(helpText: "Normal Scenario",
                    new ExtractImagesCliOptions { CohortCsvFile = "my.csv", ProjectId = "1234-5678" });
                yield return new Example(helpText: "Extract CTs without anonymisation",
                    new ExtractImagesCliOptions
                    {
                        CohortCsvFile = "my.csv",
                        ProjectId = "1234-5678",
                        Modalities = "CT",
                        IsIdentifiableExtraction = true
                    });
                yield return new Example(helpText: "Extract without applying any rejection filters",
                    new ExtractImagesCliOptions
                    { CohortCsvFile = "my.csv", ProjectId = "1234-5678", IsNoFiltersExtraction = true });
            }
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append($"ProjectId={ProjectId},");
            sb.Append($"CohortCsvFile={CohortCsvFile},");
            sb.Append($"Modalities={Modalities},");
            sb.Append($"IdentifiableExtraction={IsIdentifiableExtraction},");
            sb.Append($"NoFiltersExtraction={IsNoFiltersExtraction},");
            sb.Append($"NonInteractive={NonInteractive},");
            return sb.ToString();
        }
    }
}
