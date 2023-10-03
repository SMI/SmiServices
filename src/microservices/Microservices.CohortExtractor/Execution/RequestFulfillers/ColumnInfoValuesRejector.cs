using FAnsi.Discovery;
using MapsDirectlyToDatabaseTable;
using NLog;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.QueryBuilding;
using ReusableLibraryCode.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Common;


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

            string sql = qb.SQL;
            logger.Info("Running rejection-id fetch SQL:" + sql);

            DiscoveredTable server = columnInfo.TableInfo.Discover(DataAccessContext.DataExport);

            using (DbConnection con = server.Database.Server.GetConnection())
            {
                con.Open();
                DbCommand cmd = server.GetCommand(sql, con);
                DbDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                    toReturn.Add(reader[0].ToString()!);
            }

            logger.Info($"Found {toReturn.Count} identifiers in the reject list");

            return toReturn;
        }
    }
}
