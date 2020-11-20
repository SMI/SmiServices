using CsvHelper.Configuration.Attributes;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataFrequencyRecord : IExtractionReportCsvRecord, IEquatable<TagDataFrequencyRecord>
    {
        [UsedImplicitly]
        public uint WordLength { get; }

        [UsedImplicitly]
        public uint Count { get; }

        [UsedImplicitly]
        [Format("0.#########")]
        public double RelativeFrequencyInReport { get; }


        public TagDataFrequencyRecord(
            uint wordLength,
            uint count,
            double relativeFrequencyInReport
        )
        {
            WordLength = wordLength;
            Count = count;
            RelativeFrequencyInReport = relativeFrequencyInReport < 0 ? throw new ArgumentException(nameof(relativeFrequencyInReport)) : relativeFrequencyInReport;
        }

        public static IEnumerable<TagDataFrequencyRecord> BuildRecordList(Dictionary<uint, uint> wordLenCounts)
        {
            if (wordLenCounts.Count == 0)
                yield break;

            long totalWords = wordLenCounts.Sum(x => x.Value);
            for (uint i = 1; i <= wordLenCounts.Keys.Max(); ++i)
            {
                // Fill in any missing records with 0
                uint count = wordLenCounts.GetValueOrDefault(i, (uint)0);
                yield return new TagDataFrequencyRecord(i, count, count * 1.0 / totalWords);
            }
        }

        public override string ToString() => $"TagDataFrequencyRecord({WordLength}, {Count}, {RelativeFrequencyInReport})";

        #region Equality Members

        public bool Equals(TagDataFrequencyRecord other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                WordLength == other.WordLength
                && Count == other.Count
                && RelativeFrequencyInReport.Equals(other.RelativeFrequencyInReport);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TagDataFrequencyRecord)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                WordLength,
                Count,
                RelativeFrequencyInReport
            );
        }

        public static bool operator ==(TagDataFrequencyRecord left, TagDataFrequencyRecord right) => Equals(left, right);

        public static bool operator !=(TagDataFrequencyRecord left, TagDataFrequencyRecord right) => !Equals(left, right);

        #endregion
    }
}
