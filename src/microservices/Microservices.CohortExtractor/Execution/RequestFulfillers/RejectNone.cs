using System.Data;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class RejectNone : IRejector
    {
        public bool Reject(IDataRecord row, out string reason)
        {
            reason = null;
            return false;
        }
    }
}
