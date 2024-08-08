using System;
using System.Collections.Generic;
using System.Linq;


namespace SmiServices.Microservices.CohortPackager.ExtractJobStorage
{
    /// <summary>
    /// Provides a list of all files which were rejected from the extraction for a given extraction identifier (e.g. SeriesInstanceUID), and a reason for each rejection
    /// </summary>
    public class ExtractionIdentifierRejectionInfo
    {
        /// <summary>
        /// The ID of the key which this file was matched from
        /// </summary>
        public readonly string ExtractionIdentifier;

        // TODO(rkm 2020-10-28) This API is a bit odd -- might be more useful to get a list of file,reason from CohortExtractor instead?
        /// <summary>
        /// The list of unique reasons for files being blocked, and a count of each reason
        /// </summary>
        public readonly Dictionary<string, int> RejectionItems;


        public ExtractionIdentifierRejectionInfo(
            string keyValue,
            Dictionary<string, int> rejectionItems
        )
        {
            ExtractionIdentifier = string.IsNullOrWhiteSpace(keyValue) ? throw new ArgumentException(null, nameof(keyValue)) : keyValue;

            CheckRejectionDict(rejectionItems);
            RejectionItems = rejectionItems;
        }

        // NOTE(rkm 2020-10-27) A bit heavy-handed, but might help to track-down why some of the rejection reasons were empty in the final report
        private static void CheckRejectionDict(Dictionary<string, int> rejectionItems)
        {
            if (rejectionItems.Count == 0)
                throw new ArgumentException("Null or empty dictionary");

            if (rejectionItems.Any(x => string.IsNullOrWhiteSpace(x.Key)))
                throw new ArgumentException("Dict contains a whitespace-only key");

            List<string> zeroKeys = rejectionItems.Where(x => x.Value == 0).Select(x => x.Key).ToList();
            if (zeroKeys.Count != 0)
                throw new ArgumentException($"Dict contains key(s) with a zero count: {string.Join(',', zeroKeys)}");
        }
    }
}
