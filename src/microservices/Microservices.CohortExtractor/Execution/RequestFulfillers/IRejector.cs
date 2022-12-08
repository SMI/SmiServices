using System.Data.Common;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public interface IRejector
    {
        bool Reject(DbDataReader row, out string reason);
    }
}
