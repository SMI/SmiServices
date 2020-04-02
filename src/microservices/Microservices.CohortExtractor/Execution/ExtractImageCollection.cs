using System.Collections.Generic;
using System.Linq;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Smi.Common.Messages.Extraction;

namespace Microservices.CohortExtractor.Execution
{
    /// <summary>
    /// Results object produced by an <see cref="IExtractionRequestFulfiller"/>.
    /// </summary>
    public class ExtractImageCollection : Dictionary<string,HashSet<QueryToExecuteResult>>
    {
        /// <summary>
        /// The value of a single <see cref="ExtractionRequestMessage.ExtractionIdentifiers"/> for which we have
        /// identified results
        /// </summary>
        public string KeyValue { get; set; }

        /// <summary>
        /// Unique SeriesInstanceUIDs amongst all results stored
        /// </summary>
        public string SeriesInstanceUID => Values.SelectMany(v => v.Select(e=>e.SeriesTagValue)).Distinct().Single(); //TODO: could be multiple series under a study

        public IReadOnlyCollection<QueryToExecuteResult> Accepted => GetWhereRejected(false);
        public IReadOnlyCollection<QueryToExecuteResult> Rejected => GetWhereRejected(true);

        private IReadOnlyCollection<QueryToExecuteResult> GetWhereRejected(bool isRejected)
        {
            
            HashSet<QueryToExecuteResult> result = new HashSet<QueryToExecuteResult>();

            foreach (HashSet<QueryToExecuteResult> v in Values)
            {
                foreach (QueryToExecuteResult queryToExecuteResult in v)
                {
                    if (queryToExecuteResult.Reject == isRejected)
                        result.Add(queryToExecuteResult);
                }
            }
            return result;
        }

        public ExtractImageCollection(string keyValue)
        {
            KeyValue = keyValue;
        }
    }
}