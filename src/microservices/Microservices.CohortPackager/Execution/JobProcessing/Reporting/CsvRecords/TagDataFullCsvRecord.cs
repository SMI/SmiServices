using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords
{
    public class TagDataFullCsvRecord : IExtractionReportCsvRecord, IEquatable<TagDataFullCsvRecord>
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

        public static IEnumerable<TagDataFullCsvRecord> BuildRecordList(
            [NotNull] string tagName,
            [NotNull] Dictionary<string, List<string>> tagFailures
        )
        {
            // Order by most frequent first, then alphabetically by filename
            foreach ((string failureValue, List<string> files) in tagFailures.OrderByDescending(x => x.Value.Count))
                foreach (string file in files.OrderBy(x => x))
                    yield return new TagDataFullCsvRecord(tagName, failureValue, file);
        }

        public override string ToString() => $"TagDataFullCsvRecord({TagName}, {FailureValue}, {FilePath})";

        #region Equality members

        public bool Equals(TagDataFullCsvRecord other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                TagName == other.TagName
                && FailureValue == other.FailureValue
                && FilePath == other.FilePath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TagDataFullCsvRecord)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                TagName,
                FailureValue,
                FilePath
            );
        }

        public static bool operator ==(TagDataFullCsvRecord left, TagDataFullCsvRecord right) => Equals(left, right);

        public static bool operator !=(TagDataFullCsvRecord left, TagDataFullCsvRecord right) => !Equals(left, right);

        #endregion
    }
}
