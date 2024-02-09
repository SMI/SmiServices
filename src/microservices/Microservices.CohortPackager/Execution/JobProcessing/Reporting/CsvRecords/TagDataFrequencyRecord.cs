using CsvHelper.Configuration.Attributes;
using Equ;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataFrequencyRecord : MemberwiseEquatable<TagDataFrequencyRecord>, IExtractionReportCsvRecord
    {
        public uint WordLength { get; }

        public uint Count { get; }

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
    }
}
