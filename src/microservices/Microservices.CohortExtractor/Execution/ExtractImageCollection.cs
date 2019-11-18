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


        public ExtractImageCollection(string keyValue, string seriesInstanceUid, HashSet<string> matchingFiles)
        {
            KeyValue = keyValue;
            SeriesInstanceUID = seriesInstanceUid;
            MatchingFiles = matchingFiles;
        }
    }
}