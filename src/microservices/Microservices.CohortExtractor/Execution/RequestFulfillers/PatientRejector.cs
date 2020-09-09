using MapsDirectlyToDatabaseTable;
using NLog;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.Repositories;
using ReusableLibraryCode.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    class PatientRejector : IRejector
    {
        HashSet<string> RejectPatients = new HashSet<string>();
        private Logger _logger;

        public PatientRejector(ColumnInfo patientIdColumn)
        {
            _logger = LogManager.GetCurrentClassLogger();

            var qb = new QueryBuilder(null,null);
            qb.AddColumn(new ColumnInfoToIColumn(new MemoryRepository(),patientIdColumn));
            
            var sql = qb.SQL;
            _logger.Info("Running PatientID fetch SQL:" + sql);

            var server = patientIdColumn.TableInfo.Discover(DataAccessContext.DataExport);

            using(var con = server.Database.Server.GetConnection())
            {
                var cmd = server.GetCommand(sql,con);
                var r = cmd.ExecuteReader();

                while(r.Read())
                    RejectPatients.Add(r[0].ToString());
            }
            
            _logger.Info($"Found {RejectPatients.Count} patients in the reject list");
        }


        public bool Reject(DbDataReader row, out string reason)
        {
            string patientId;

            try
            {
                //we don't know
                if(row["PatientID"] == DBNull.Value)
                {
                    reason = null;
                    return false;
                }

                patientId = (string)row["PatientID"];
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred determining the PatientID of the record(s) being extracted",ex);
            }
            
            if(RejectPatients.Contains(patientId))
            {
                reason = "Patient was in reject list";
                return true;
            }
                

            reason = null;
            return false;
        }
    }
}
