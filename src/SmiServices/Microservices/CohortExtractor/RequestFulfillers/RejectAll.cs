using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers;

// NOTE: Only used by IntegrationTest_Rejector
public class RejectAll : IRejector
{
    public bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason)
    {
        reason = "Rejector is " + nameof(RejectAll);
        return true;
    }
}
