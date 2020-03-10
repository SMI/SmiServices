using System.Collections.Generic;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out
{
    class ProblemValuesUpdateStrategy : IUpdateStrategy
    {
        public IEnumerable<string> GetUpdateSql(DiscoveredTable table,
            Dictionary<DiscoveredTable, DiscoveredColumn> primaryKeys, Failure failure, IsIdentifiableRule usingRule)
        {
            var syntax = table.GetQuerySyntaxHelper();

            foreach (var part in failure.Parts)
            {

                yield return 
                    $@"update {table.GetFullyQualifiedName()} 
                SET {syntax.EnsureWrapped(failure.ProblemField)} = 
                REPLACE({syntax.EnsureWrapped(failure.ProblemField)},'{syntax.Escape(part.Word)}', 'SMI_REDACTED')
                WHERE {primaryKeys[table].GetFullyQualifiedName()} = '{syntax.Escape(failure.ResourcePrimaryKey)}'";
            }
        }
    }
}