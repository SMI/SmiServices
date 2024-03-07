using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class RejectNone : IRejector
    {
        public bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason)
        {
            reason = null;
            return false;
        }
    }
}
