using Rdmp.Core.MapsDirectlyToDatabaseTable;
using NLog;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using System;
using System.Collections.Generic;


namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{

    public class ColumnInfoValuesRejector : ColumnValuesRejector
    {

        public ColumnInfoValuesRejector(ColumnInfo columnInfo) : base(columnInfo.GetRuntimeName(),FetchTable(columnInfo))
        {

        }

        private static HashSet<string> FetchTable(ColumnInfo columnInfo)
        {
            var logger = LogManager.GetCurrentClassLogger();
            HashSet<string> toReturn = new(StringComparer.CurrentCultureIgnoreCase);

            var qb = new QueryBuilder(limitationSQL: null, hashingAlgorithm: null);
            qb.AddColumn(new ColumnInfoToIColumn(new MemoryRepository(), columnInfo));

            var sql = qb.SQL;
            logger.Info($"Running rejection-id fetch SQL:{sql}");

            var server = columnInfo.TableInfo.Discover(DataAccessContext.DataExport);

            using (var con = server.Database.Server.GetConnection())
            {
                con.Open();
                var cmd = server.GetCommand(sql, con);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                    toReturn.Add(reader[0].ToString()!);
            }

            logger.Info($"Found {toReturn.Count} identifiers in the reject list");

            return toReturn;
        }
    }
}
