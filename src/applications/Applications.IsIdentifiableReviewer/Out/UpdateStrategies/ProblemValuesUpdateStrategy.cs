using System.Collections.Generic;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out.UpdateStrategies
{
    /// <summary>
    /// builds SQL UPDATE statements based on the fixed strings in <see cref="FailurePart.Word"/>
    /// </summary>
    public class ProblemValuesUpdateStrategy : UpdateStrategy
    {
        /// <summary>
        /// Generates 1 UPDATE statement per <see cref="Failure.Parts"/> for redacting the current <paramref name="failure"/>
        /// </summary>
        /// <param name="table"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="failure"></param>
        /// <param name="usingRule"></param>
        /// <returns></returns>
        public override IEnumerable<string> GetUpdateSql(DiscoveredTable table,
            Dictionary<DiscoveredTable, DiscoveredColumn> primaryKeys, Failure failure, IsIdentifiableRule usingRule)
        {
            var syntax = table.GetQuerySyntaxHelper();

            foreach (var part in failure.Parts)
            {

                yield return GetUpdateWordSql(table, primaryKeys,syntax, failure, part.Word);
            }
        }
    }
}
