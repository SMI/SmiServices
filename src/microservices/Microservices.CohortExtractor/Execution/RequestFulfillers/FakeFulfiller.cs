
using Microservices.CohortExtractor.Audit;
using Smi.Common.Messages.Extraction;
using NLog;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Smi.Common.Options;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    /// <summary>
    /// Fake <see cref="IExtractionRequestFulfiller"/> that automatically finds all UIDs that you ask it to look up and returns
    /// a single image name for each requested UID.  The filename will be the UID(s) you asked for
    /// </summary>
    public class FakeFulfiller : IExtractionRequestFulfiller
    {
        protected readonly Logger Logger;

        public List<IRejector> Rejectors { get; set; } = new List<IRejector>();
        
        public Regex ModalityRoutingRegex { get; set; }
        public Dictionary<ModalitySpecificRejectorOptions, IRejector> ModalitySpecificRejectors { get; set; }
            = new Dictionary<ModalitySpecificRejectorOptions, IRejector>();

        public FakeFulfiller()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message, IAuditExtractions auditor)
        {
            Logger.Debug("Found " + message.KeyTag);

            foreach (string valueToLookup in message.ExtractionIdentifiers)
            {
                var results = new ExtractImageCollection(valueToLookup);
                string filePathValue = valueToLookup; // "img001.dcm";
                string studyTagValue = "2";
                string seriesTagValue = "3";
                string instanceTagValue = "4";
                bool rejection = false;
                string rejectionReason = "";
                var result = new QueryToExecuteResult(filePathValue, studyTagValue, seriesTagValue, instanceTagValue, rejection, rejectionReason);
                if(!results.ContainsKey(result.SeriesTagValue))
                    results.Add(result.SeriesTagValue,new HashSet<QueryToExecuteResult>());
                results[result.SeriesTagValue].Add(result);

                yield return results;
            }
        }
    }
}