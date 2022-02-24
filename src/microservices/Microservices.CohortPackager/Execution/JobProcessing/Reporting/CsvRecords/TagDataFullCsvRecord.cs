using Equ;
using JetBrains.Annotations;
using Microservices.IsIdentifiable.Failures;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataFullCsvRecord : MemberwiseEquatable<TagDataFullCsvRecord>, IExtractionReportCsvRecord
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
        /// The path to the file which contained the failure, relative to the extraction directory
        /// </summary>
        [NotNull]
        [UsedImplicitly]
        public string FilePath { get; }


        public TagDataFullCsvRecord(
            [NotNull] string tagName,
            [NotNull] string failureValue,
            int offset,
            FailureClassification failureClassification,
            [NotNull] string filePath
        )
        {
            TagName = string.IsNullOrWhiteSpace(tagName) ? throw new ArgumentException(nameof(tagName)) : tagName;
            FailureValue = string.IsNullOrWhiteSpace(failureValue) ? throw new ArgumentException(nameof(failureValue)) : failureValue;
            Offset = (offset < -1) ? throw new ArgumentException(nameof(offset)) : offset;
            Classification = (failureClassification == FailureClassification.None) ? throw new ArgumentException(nameof(failureClassification)) : failureClassification;
            FilePath = string.IsNullOrWhiteSpace(filePath) ? throw new ArgumentException(nameof(filePath)) : filePath;
        }
        /// <summary>
        /// Convert the input list of (filepath, FailureData) tuples for the given tag into an enumerable of TagDataFullCsvRecord
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagFailures"></param>
        /// <returns></returns>
        public static IEnumerable<TagDataFullCsvRecord> BuildRecordList(
            [NotNull] string tagName,
            [NotNull] IEnumerable<Tuple<string, FailureData>> tagFailures
        )
        {
            foreach (
                (string filePath, FailureData failureData) in
                // Order by highest failure parts first
                tagFailures.OrderByDescending(x => x.Item2.Parts.Count)
            )
            {
                foreach (
                    var thing in
                    failureData.Parts
                        // Order by increasing offset
                        .OrderBy(x => x.Offset)
                )
                {
                    yield return new TagDataFullCsvRecord(
                        tagName,
                        thing.Word,
                        thing.Offset,
                        thing.Classification,
                        filePath
                    );
                }
            }
        }

        public override string ToString() => $"TagDataFullCsvRecord({TagName}, {FailureValue}, {Offset}, {Classification}, {FilePath})";
    }
}
