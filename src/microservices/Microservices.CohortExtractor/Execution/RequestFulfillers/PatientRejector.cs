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
    public class PatientRejector : IRejector
    {
        private readonly HashSet<string> _rejectPatients = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        private readonly Logger _logger;
        private readonly string _patientIdColumnName;

        public PatientRejector(ColumnInfo patientIdColumn)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _patientIdColumnName = patientIdColumn.GetRuntimeName();

            var qb = new QueryBuilder(limitationSQL: null, hashingAlgorithm: null);
            qb.AddColumn(new ColumnInfoToIColumn(new MemoryRepository(), patientIdColumn));

            string sql = qb.SQL;
            _logger.Info("Running PatientID fetch SQL:" + sql);

            DiscoveredTable server = patientIdColumn.TableInfo.Discover(DataAccessContext.DataExport);

            using (DbConnection con = server.Database.Server.GetConnection())
            {
                con.Open();
                DbCommand cmd = server.GetCommand(sql, con);
                DbDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                    _rejectPatients.Add(reader[0].ToString());
            }

            _logger.Info($"Found {_rejectPatients.Count} patients in the reject list");
        }


        public bool Reject(DbDataReader row, out string reason)
        {
            string patientId;

            try
            {
                // The patient ID is null
                if (row[_patientIdColumnName] == DBNull.Value)
                {
                    reason = null;
                    return false;
                }

                patientId = (string)row[_patientIdColumnName];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new IndexOutOfRangeException($"An error occurred determining the PatientID of the record(s) being extracted. Expected a column called {_patientIdColumnName}", ex);
            }

            if (_rejectPatients.Contains(patientId))
            {
                reason = "Patient was in reject list";
                return true;
            }

            reason = null;
            return false;
        }
    }
}
