using Equ;
using JetBrains.Annotations;
using Microservices.IsIdentifiable.Failures;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataSummaryCsvRecord : MemberwiseEquatable<TagDataSummaryCsvRecord>, IExtractionReportCsvRecord
    {
        /// <summary>
        /// The tag name which contained the failure value 
        /// </summary>
        [NotNull]
        [UsedImplicitly]
        public string TagName { get; }

        /// <summary>
        /// This particular failure value
        /// </summary>
        [NotNull]
        [UsedImplicitly]
        public string FailureValue { get; }

        /// <summary>
        /// The offset of the value in the tag value, or -1 if not applicable (e.g. in pixel data)
        /// </summary>
        [UsedImplicitly]
        public int Offset { get; }

        /// <summary>
        /// The value classification
        /// </summary>
        [NotNull]
        [UsedImplicitly]
        public FailureClassification Classification { get; }

        /// <summary>
        /// The total number of occurrences of this exact failure for the specific tag
        /// </summary>
        [UsedImplicitly]
        public uint Occurrences { get; }

        /// <summary>
        /// The relative frequency of this failure value across the tag
        /// </summary>
        [UsedImplicitly]
        public double RelativeFrequencyInTag { get; }

        /// <summary>
        /// The relative frequency of this value across the whole report
        /// </summary>
        // NOTE(rkm 2020-10-29) This is set elsewhere before the record is written-out
        private double _relativeFrequencyInReport;
        public double RelativeFrequencyInReport
        {
            get => _relativeFrequencyInReport;
            set
            {
                if (_relativeFrequencyInReport != 0.0)
                    throw new ArgumentException("RelativeFrequencyInReport already set for record");
                _relativeFrequencyInReport = value;
            }
        }


        public TagDataSummaryCsvRecord(
            [NotNull] string tagName,
            [NotNull] string failureValue,
            [NotNull] int offset,
            [NotNull] FailureClassification failureClassification,
            uint occurrences,
            double frequency
        )
        {
            TagName = string.IsNullOrWhiteSpace(tagName) ? throw new ArgumentException(nameof(tagName)) : tagName;
            FailureValue = string.IsNullOrWhiteSpace(failureValue) ? throw new ArgumentException(nameof(failureValue)) : failureValue;
            Offset = (offset < -1) ? throw new ArgumentException(nameof(offset)) : offset;
            Classification = (failureClassification == FailureClassification.None) ? throw new ArgumentException(nameof(failureClassification)) : failureClassification;
            Occurrences = occurrences == 0 ? throw new ArgumentException(nameof(occurrences)) : occurrences;
            RelativeFrequencyInTag = frequency <= 0 ? throw new ArgumentException(nameof(frequency)) : frequency;
        }

        /// <summary>
        /// Convert the input list of FailureData items for the given tag into an enumerable of TagDataSummaryCsvRecord
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagFailures"></param>
        /// <returns></returns>
        public static IEnumerable<TagDataSummaryCsvRecord> BuildRecordList(string tagName, IEnumerable<FailureData> tagFailures)
        {
            List<FailurePart> allParts = tagFailures.SelectMany(x => x.Parts).ToList();
            foreach (
                IGrouping<FailurePart, FailurePart> partsGroup in
                allParts
                    // Group by FailurePart
                    .GroupBy(x => x)
                    // Order by most common FailurePart first
                    .OrderByDescending(x => x.Count())
            )
            {
                var part = partsGroup.Key;
                var partCount = partsGroup.Count();

                yield return new TagDataSummaryCsvRecord(
                    tagName,
                    part.Word,
                    part.Offset,
                    part.Classification,
                    (uint)partCount,
                    partCount * 1.0 / allParts.Count()
                );
            }
        }

        public override string ToString() => $"TagDataSummaryCsvRecord({TagName}, {FailureValue}, {Offset}, {Classification}, {Occurrences}, {RelativeFrequencyInTag}, {RelativeFrequencyInReport})";
    }
}
