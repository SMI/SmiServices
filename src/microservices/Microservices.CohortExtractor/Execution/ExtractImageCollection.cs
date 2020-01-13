using System.Collections.Generic;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Smi.Common.Messages.Extraction;

namespace Microservices.CohortExtractor.Execution
{
    /// <summary>
    /// Results object produced by an <see cref="IExtractionRequestFulfiller"/>.
    /// </summary>
    public class ExtractImageCollection
    {
        /// <summary>
        /// The value of a single <see cref="ExtractionRequestMessage.ExtractionIdentifiers"/> for which we have
        /// identified the available <see cref="MatchingFiles"/>.
        /// </summary>
        public string KeyValue { get; set; }
        
        public string SeriesInstanceUID { get; set; }

        public HashSet<string> MatchingFiles { get; set; }

        /// <summary>
        /// List of SOPInstanceUID that would be part of the <see cref="MatchingFiles"/> but which were for some reason
        /// not extracted.  The Key is the SOPInstanceUID and the Value is the reason for rejection
        /// </summary>
        public Dictionary<string,string> Rejections { get; set; } = new Dictionary<string, string>();
        
        public ExtractImageCollection(string keyValue, string seriesInstanceUid, HashSet<string> matchingFiles)
        {
            KeyValue = keyValue;
            SeriesInstanceUID = seriesInstanceUid;
            MatchingFiles = matchingFiles;
        }
    }
}