using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    public class RejectAll : IRejector
    {
        public bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason)
        {
            reason = "Rejector is " + nameof(RejectAll);
            return true;
        }
    }
}
