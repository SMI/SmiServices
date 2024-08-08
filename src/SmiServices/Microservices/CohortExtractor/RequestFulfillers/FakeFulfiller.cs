using NLog;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortExtractor.Audit;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    /// <summary>
    /// Fake <see cref="IExtractionRequestFulfiller"/> that automatically finds all UIDs that you ask it to look up and returns
    /// a single image name for each requested UID.  The filename will be the UID(s) you asked for
    /// </summary>
    public class FakeFulfiller : IExtractionRequestFulfiller
    {
        protected readonly Logger Logger;

        public List<IRejector> Rejectors { get; set; } = [];

        public Regex? ModalityRoutingRegex { get; set; }
        public Dictionary<ModalitySpecificRejectorOptions, IRejector> ModalitySpecificRejectors { get; set; }
            = [];

        public FakeFulfiller()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message, IAuditExtractions auditor)
        {
            Logger.Debug($"Found {message.KeyTag}");

            foreach (var valueToLookup in message.ExtractionIdentifiers)
            {
                var results = new ExtractImageCollection(valueToLookup);
                var studyTagValue = "2";
                var seriesTagValue = "3";
                var instanceTagValue = "4";
                var rejection = false;
                var rejectionReason = "";
                var result = new QueryToExecuteResult(valueToLookup, studyTagValue, seriesTagValue, instanceTagValue, rejection, rejectionReason);
                if (!results.ContainsKey(result.SeriesTagValue!))
                    results.Add(result.SeriesTagValue!, []);
                results[result.SeriesTagValue!].Add(result);

                yield return results;
            }
        }
    }
}
