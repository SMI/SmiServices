using System.Collections.Generic;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out.UpdateStrategies
{
    public interface IUpdateStrategy
    {
        IEnumerable<string> GetUpdateSql(DiscoveredTable table,
            Dictionary<DiscoveredTable, DiscoveredColumn> primaryKeys, Failure failure, IsIdentifiableRule usingRule);
    }
}