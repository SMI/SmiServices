using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.Operations;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.LoadExecution.Components;
using ReusableLibraryCode.Progress;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microservices.DicomRelationalMapper.Execution
{
    internal class MigrateRawToStagingWithSelectIntoStatements : DataLoadComponent
    {
        
        public override ExitCodeType Run(IDataLoadJob job, GracefulCancellationToken cancellationToken)
        {
            if (Skip(job))
                return ExitCodeType.Error;

            var configuration = job.Configuration;
            var namer = configuration.DatabaseNamer;

            // To be on the safe side, we will create/destroy the staging tables on a per-load basis
            var cloner = new DatabaseCloner(configuration);
            job.CreateTablesInStage(cloner, LoadBubble.Staging);

            DiscoveredServer server = job.LoadMetadata.GetDistinctLiveDatabaseServer();
            server.EnableAsync();

            using (DbConnection con = server.GetConnection())
            {
                con.Open();

                var running = new List<Task>();

                Stopwatch sw = Stopwatch.StartNew();

                foreach (TableInfo table in job.RegularTablesToLoad)
                {
                    string fromDb = table.GetDatabaseRuntimeName(LoadStage.AdjustRaw, namer);
                    string toDb = table.GetDatabaseRuntimeName(LoadStage.AdjustStaging, namer);

                    string fromTable = table.GetRuntimeName(LoadStage.AdjustRaw, namer);
                    string toTable = table.GetRuntimeName(LoadStage.AdjustStaging, namer);

                    IQuerySyntaxHelper syntaxHelper = table.GetQuerySyntaxHelper();

                    string sql = string.Format(@"INSERT INTO {1} SELECT DISTINCT * FROM {0}",
                        syntaxHelper.EnsureFullyQualified(fromDb, null, fromTable),
                        syntaxHelper.EnsureFullyQualified(toDb, null, toTable));

                    job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to send SQL:" + sql));


                    DbCommand cmd = server.GetCommand(sql, con);
                    running.Add(cmd.ExecuteNonQueryAsync());
                }

                sw.Stop();
                Task.WaitAll(running.ToArray());

                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Migrated all rows using INSERT INTO in " + sw.ElapsedMilliseconds + "ms"));
            }
            return ExitCodeType.Success;
        }
    }
}