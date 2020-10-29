using JetBrains.Annotations;
using System;
using System.Collections.Generic;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    // TODO tests
    public class TagDataFullCsvRecord : IExtractionReportCsvRecord
    {
        /// <summary>
        /// The tag name which contained the failure value 
        /// </summary>
        [NotNull]
        [UsedImplicitly]
        public string TagName { get; }

        /// <summary>
        /// The value which has been recorded as a validation failure
        /// </summary>
        [NotNull]
        [UsedImplicitly]
        public string FailureValue { get; }

        /// <summary>
        /// The path to the file which contained the failure, relative to the extraction directory
        /// </summary>
        [NotNull]
        [UsedImplicitly]
        public string FilePath { get; }


        public TagDataFullCsvRecord(
            [NotNull] string tagName,
            [NotNull] string failureValue,
            [NotNull] string filePath
        )
        {
            TagName = string.IsNullOrWhiteSpace(tagName) ? throw new ArgumentException(nameof(tagName)) : tagName;
            FailureValue = string.IsNullOrWhiteSpace(failureValue) ? throw new ArgumentException(nameof(failureValue)) : failureValue;
            FilePath = string.IsNullOrWhiteSpace(filePath) ? throw new ArgumentException(nameof(filePath)) : filePath;
        }

        public static IEnumerable<TagDataFullCsvRecord> BuildRecordList(string tagName, Dictionary<string, List<string>> tagFailures)
        {
            foreach ((string failureValue, List<string> files) in tagFailures)
                foreach (string file in files)
                    yield return new TagDataFullCsvRecord(tagName, failureValue, file);
        }
    }
}
