using System.Data.Common;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class RejectNone : IRejector
    {
        public bool Reject(DbDataReader row, out string reason)
        {
            reason = null;
            return false;
        }
    }
}
