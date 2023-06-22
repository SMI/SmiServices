using System.Data;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class RejectAll : IRejector
    {
        public bool Reject(IDataRecord row, out string reason)
        {
            reason = "Rejector is " + nameof(RejectAll);
            return true;
        }
    }
}
