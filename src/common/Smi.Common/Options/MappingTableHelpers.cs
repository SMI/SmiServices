using FAnsi.Discovery;
using System;

namespace Smi.Common.Options
{
    public static class MappingTableHelpers
    {
        public static DiscoveredTable DiscoverTable(IMappingTableOptions options)
        {
            var server = new DiscoveredServer(options.MappingConnectionString, options.MappingDatabaseType);

            var idx = options.MappingTableName.LastIndexOf('.');
            var tableNameUnqualified = options.MappingTableName[(idx + 1)..];

            idx = options.MappingTableName.IndexOf('.');
            if (idx == -1)
                throw new ArgumentException($"MappingTableName did not contain the database/user section:'{options.MappingTableName}'");

            var databaseName = server.GetQuerySyntaxHelper().GetRuntimeName(options.MappingTableName[..idx]);
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException($"Could not get database/username from MappingTableName {options.MappingTableName}");

            return server.ExpectDatabase(databaseName).ExpectTable(tableNameUnqualified);
        }
    }
}
