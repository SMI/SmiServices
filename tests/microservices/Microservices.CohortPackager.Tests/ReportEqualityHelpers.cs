using JetBrains.Annotations;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
using Smi.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;


namespace Microservices.CohortPackager.Tests
{
    public static class ReportEqualityHelpers
    {
        [UsedImplicitly] // Can be set to help debug test output
        public static bool ShouldPrintReports { get; set; }

        public static void AssertReportsAreEqual(
            CompletedExtractJobInfo jobInfo,
            DateTimeProvider provider,
            Dictionary<string, Dictionary<string, List<string>>>? verificationFailuresExpected,
            Dictionary<string, List<Tuple<int, string>>>? blockedFilesExpected,
            List<Tuple<string, string>>? anonFailuresExpected,
            bool isIdentifiableExtraction,
            bool isJoinedReport,
            string newLine,
            string actualReport
        )
        {
            string header = GetHeaderAndContents(jobInfo, provider, newLine);

            if (isIdentifiableExtraction)
            {
                Assert.NotNull(anonFailuresExpected);
                IEnumerable<string> missingFiles = anonFailuresExpected!.Select(x => x.Item1);
                CheckIdentReport(header, missingFiles, newLine, actualReport);
                return;
            }

            (
                string? expectedVerificationFailuresSummary,
                string? expectedVerificationFailuresFull
            ) = ExpectedVerificationFailures(verificationFailuresExpected, newLine);

            var expected = new List<string>
            {
                header,
                $"",
                $"## Verification failures",
                $"",
                $"### Summary",
                $"",
                expectedVerificationFailuresSummary ?? "",
                $"### Full details",
                $"",
                expectedVerificationFailuresFull ?? "",
                $"## Blocked files",
                $"",
                BlockedFiles(blockedFilesExpected, newLine) ?? "",
                $"## Anonymisation failures",
                $"",
                AnonymisationFailures(anonFailuresExpected, newLine) ?? "",
                $"--- end of report ---",
                $"",
            };

            string expectedStr = string.Join(newLine, expected);

            if (ShouldPrintReports)
                PrintReports(expectedStr, actualReport);

            Assert.AreEqual(expectedStr, actualReport);
        }

        private static string GetHeaderAndContents(ExtractJobInfo jobInfo, DateTimeProvider provider, string newLine)
        {
            string identExtraction = jobInfo.IsIdentifiableExtraction ? "Yes" : "No";
            string filteredExtraction = !jobInfo.IsNoFilterExtraction ? "Yes" : "No";

            var headerLines = new List<string>
            {
                $"# SMI extraction validation report for 1234/test",
                $"",
                $"Job info:",
                $"-   Job submitted at:             {provider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {(provider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {TimeSpan.FromHours(1)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               {jobInfo.KeyTag}",
                $"-   Extraction modality:          {jobInfo.ExtractionModality ?? "Unspecified"}",
                $"-   Requested identifier count:   {jobInfo.KeyValueCount}",
                $"-   Identifiable extraction:      {identExtraction}",
                $"-   Filtered extraction:          {filteredExtraction}",
                $"",
                $"Report contents:",
                $"",
            };

            if (!jobInfo.IsIdentifiableExtraction)
            {
                headerLines.AddRange(new List<string>
                {
                    $"-   Verification failures",
                    $"    -   Summary",
                    $"    -   Full Details",
                    $"-   Blocked files",
                    $"-   Anonymisation failures",
                });
            }
            else
            {
                headerLines.AddRange(new List<string>
                {
                    $"-   Missing file list (files which were selected from an input ID but could not be found)",
                });
            }

            return string.Join(newLine, headerLines);
        }

        private static void CheckIdentReport(string headerLines, IEnumerable<string?> missingFiles, string newLine, string actualReport)
        {
            var expected = new List<string>
            {
                headerLines,
                $"",
                $"## Missing file list",
                $"",
                string.Join(newLine, missingFiles.Select(x => $"-   {x}")),
                $"",
                $"--- end of report ---",
                $"",
            };

            string expectedStr = string.Join(newLine, expected);
            PrintReports(expectedStr, actualReport);
            Assert.AreEqual(expectedStr, actualReport);
        }

        private static Tuple<string?, string?> ExpectedVerificationFailures(Dictionary<string, Dictionary<string, List<string>>>? verificationFailuresExpected, string newLine)
        {
            if (verificationFailuresExpected == null)
                return new Tuple<string?, string?>(null, null);

            var summarySb = new StringBuilder();
            var fullSb = new StringBuilder();

            List<KeyValuePair<string, Dictionary<string, List<string>>>> ordered = verificationFailuresExpected.OrderByDescending(x => x.Value.Sum(y => y.Value.Count)).ToList();
            // NOTE(rkm 2020-11-18) Shift PixelData to the end
            KeyValuePair<string, Dictionary<string, List<string>>> pixels = ordered.SingleOrDefault(x => x.Key == "PixelData");
            if (pixels.Key != null)
            {
                ordered.Remove(pixels);
                ordered.Add(pixels);
            }

            foreach ((string key, Dictionary<string, List<string>> values) in ordered)
            {
                int totalOccurrences = values.Sum(x => x.Value.Count);
                summarySb.Append($"-   Tag: {key} ({totalOccurrences} total occurrence(s)){newLine}");
                fullSb.Append($"-   Tag: {key} ({totalOccurrences} total occurrence(s)){newLine}");
                foreach ((string value, List<string> inFiles) in values)
                {
                    totalOccurrences = inFiles.Count;
                    summarySb.Append($"    -   Value: '{value}' ({totalOccurrences} occurrence(s)){newLine}");
                    fullSb.Append($"    -   Value: '{value}' ({totalOccurrences} occurrence(s)){newLine}");
                    foreach (string fileName in inFiles)
                        fullSb.Append($"        -   {fileName}{newLine}");
                }

                summarySb.Append($"{newLine}");
                fullSb.Append($"{newLine}");
            }
            return new Tuple<string?, string?>(summarySb.ToString(), fullSb.ToString());
        }

        private static string BlockedFiles(Dictionary<string, List<Tuple<int, string>>>? blockedFilesExpected, string newLine)
        {
            if (blockedFilesExpected == null)
                return null;

            var sb = new StringBuilder();
            foreach ((string id, List<Tuple<int, string>> blockedItems) in blockedFilesExpected.OrderByDescending(x => x.Value.Sum(y => y.Item1)))
            {
                sb.Append($"-   ID: {id}{newLine}");
                foreach ((int count, string reason) in blockedItems.OrderByDescending(x => x.Item1))
                    sb.Append($"    -   {count}x '{reason}'{newLine}");
            }
            return sb.ToString();
        }

        private static string AnonymisationFailures(List<Tuple<string, string>>? anonFailuresExpected, string newLine)
        {
            if (anonFailuresExpected == null)
                return null;

            var sb = new StringBuilder();
            foreach ((string file, string reason) in anonFailuresExpected)
                sb.Append($"-   file '{file}': '{reason}'{newLine}");
            return sb.ToString();
        }

        private static void PrintReports(string expected, string actual)
        {
            Console.WriteLine("--- expected ---");
            Console.WriteLine(expected);
            Console.WriteLine("--- actual ---");
            Console.WriteLine(actual);
        }
    }
}
