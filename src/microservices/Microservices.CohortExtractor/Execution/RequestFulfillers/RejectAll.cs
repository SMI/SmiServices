using System.Data;
using System.Data.Common;

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
