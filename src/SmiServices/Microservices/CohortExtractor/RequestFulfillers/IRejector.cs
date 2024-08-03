using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    public interface IRejector
    {
        /// <summary>
        /// Test whether the given <paramref name="row"/> should be rejected
        /// </summary>
        /// <param name="row"></param>
        /// <param name="reason">The reason for rejection, if any</param>
        /// <returns>True if the record was rejected, else false</returns>
        bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason);
    }
}
