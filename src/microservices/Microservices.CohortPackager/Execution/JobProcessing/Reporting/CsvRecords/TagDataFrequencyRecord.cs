using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    // TODO Tests
    public class TagDataFrequencyRecord : IExtractionReportCsvRecord
    {
        [UsedImplicitly]
        public int WordLength { get; }

        [UsedImplicitly]
        public int Count { get; }

        [UsedImplicitly]
        public double Frequency { get; }


        public TagDataFrequencyRecord(
            int wordLength,
            int count,
            double frequency
        )
        {
            WordLength = wordLength < 0 ? throw new ArgumentException(nameof(wordLength)) : wordLength;
            Count = count < 0 ? throw new ArgumentException(nameof(count)) : count;
            Frequency = frequency < 0 ? throw new ArgumentException(nameof(frequency)) : frequency;
        }
        public static IEnumerable<TagDataFrequencyRecord> BuildRecordList(Dictionary<int, int> wordLenCounts)
        {
            if (wordLenCounts.Count == 0)
                yield break;

            for (var i = 1; i <= wordLenCounts.Keys.Max(); ++i)
            {
                // Fill in any missing records with 0
                int count = wordLenCounts.GetValueOrDefault(i, 0);
                yield return new TagDataFrequencyRecord(i, count, count * 1.0 / wordLenCounts.Count);
            }
        }
    }
}
