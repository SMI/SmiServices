using System.Data.Common;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class RejectAll : IRejector
    {
        public bool Reject(DbDataReader row, out string reason)
        {
            reason = "Rejector is " + nameof(RejectAll);
            return true;
        }
    }
}
