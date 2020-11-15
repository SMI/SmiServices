using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    // TODO tests
    public class TagDataSummaryCsvRecord : IExtractionReportCsvRecord
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
        /// The total number of occurrences of this value for the specific tag
        /// </summary>
        [UsedImplicitly]
        public int Occurrences { get; }

        /// <summary>
        /// The relative frequency of this value across the tag
        /// </summary>
        [UsedImplicitly]
        public double RelativeFrequencyInTag { get; }

        /// <summary>
        /// The relative frequency of this value across the whole report
        /// </summary>
        // NOTE(rkm 2020-10-29) This is set elsewhere before the record is written-out
        public double RelativeFrequencyInReport { get; set; }

        public TagDataSummaryCsvRecord(
            [NotNull] string tagName,
            [NotNull] string failureValue,
            int occurrences,
            double frequency
        )
        {
            TagName = string.IsNullOrWhiteSpace(tagName) ? throw new ArgumentException(nameof(tagName)) : tagName;
            FailureValue = string.IsNullOrWhiteSpace(failureValue) ? throw new ArgumentException(nameof(failureValue)) : failureValue;
            Occurrences = occurrences <= 0 ? throw new ArgumentException(nameof(occurrences)) : occurrences;
            RelativeFrequencyInTag = frequency <= 0 ? throw new ArgumentException(nameof(frequency)) : frequency;
        }

        public static IEnumerable<TagDataSummaryCsvRecord> BuildRecordList(string tagName, Dictionary<string, List<string>> tagFailures)
        {
            int totalInstances = tagFailures.Sum(x => x.Value.Count);
            foreach ((string failureValue, List<string> files) in tagFailures)
                yield return new TagDataSummaryCsvRecord(tagName, failureValue, files.Count, files.Count * 1.0 / totalInstances);
        }
    }
}
