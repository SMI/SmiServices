using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataSummaryCsvRecord : IExtractionReportCsvRecord, IEquatable<TagDataSummaryCsvRecord>
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
        public uint Occurrences { get; }

        /// <summary>
        /// The relative frequency of this value across the tag
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
            uint occurrences,
            double frequency
        )
        {
            TagName = string.IsNullOrWhiteSpace(tagName) ? throw new ArgumentException(nameof(tagName)) : tagName;
            FailureValue = string.IsNullOrWhiteSpace(failureValue) ? throw new ArgumentException(nameof(failureValue)) : failureValue;
            Occurrences = occurrences == 0 ? throw new ArgumentException(nameof(occurrences)) : occurrences;
            RelativeFrequencyInTag = frequency <= 0 ? throw new ArgumentException(nameof(frequency)) : frequency;
        }

        public static IEnumerable<TagDataSummaryCsvRecord> BuildRecordList(string tagName, Dictionary<string, List<string>> tagFailures)
        {
            int totalInstances = tagFailures.Sum(x => x.Value.Count);
            foreach ((string failureValue, List<string> files) in tagFailures.OrderByDescending(x => x.Value.Count))
                yield return new TagDataSummaryCsvRecord(tagName, failureValue, (uint)files.Count, files.Count * 1.0 / totalInstances);
        }

        public override string ToString() => $"TagDataSummaryCsvRecord({TagName}, {FailureValue}, {Occurrences}, {RelativeFrequencyInTag}, {RelativeFrequencyInReport})";

        #region Equality members

        public bool Equals(TagDataSummaryCsvRecord other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                TagName == other.TagName
                && FailureValue == other.FailureValue
                && Occurrences == other.Occurrences
                && RelativeFrequencyInTag.Equals(other.RelativeFrequencyInTag)
                && RelativeFrequencyInReport.Equals(other.RelativeFrequencyInReport);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TagDataSummaryCsvRecord)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                TagName,
                FailureValue,
                Occurrences,
                RelativeFrequencyInTag,
                RelativeFrequencyInReport
            );
        }

        public static bool operator ==(TagDataSummaryCsvRecord left, TagDataSummaryCsvRecord right) => Equals(left, right);

        public static bool operator !=(TagDataSummaryCsvRecord left, TagDataSummaryCsvRecord right) => !Equals(left, right);

        #endregion
    }
}
