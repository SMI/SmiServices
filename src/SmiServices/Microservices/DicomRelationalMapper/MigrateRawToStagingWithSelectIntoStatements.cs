using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.Operations;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.LoadExecution.Components;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Diagnostics;
using System.Linq;

namespace SmiServices.Microservices.DicomRelationalMapper;

internal class MigrateRawToStagingWithSelectIntoStatements : DataLoadComponent
{

    public override ExitCodeType Run(IDataLoadJob job, GracefulCancellationToken cancellationToken)
    {
        if (Skip(job))
            return ExitCodeType.Error;

        var configuration = job.Configuration;
        var namer = configuration.DatabaseNamer;

        var server = job.LoadMetadata.GetDistinctLiveDatabaseServer();

        //Drop any STAGING tables that already exist
        foreach (var table in job.RegularTablesToLoad)
        {
            var stagingDbName = table.GetDatabaseRuntimeName(LoadStage.AdjustStaging, namer);
            var stagingTableName = table.GetRuntimeName(LoadStage.AdjustStaging, namer);

            var stagingDb = server.ExpectDatabase(stagingDbName);
            var stagingTable = stagingDb.ExpectTable(stagingTableName);

            if (stagingDb.Exists() && stagingTable.Exists())
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Dropping existing STAGING table remnant {stagingTable.GetFullyQualifiedName()}"));
                stagingTable.Drop();
            }
        }

        //Now create STAGING tables (empty)
        var cloner = new DatabaseCloner(configuration);
        job.CreateTablesInStage(cloner, LoadBubble.Staging);


        using var con = server.GetConnection();
        con.Open();

        var sw = Stopwatch.StartNew();

        foreach (TableInfo table in job.RegularTablesToLoad.Cast<TableInfo>())
        {
            var fromDb = table.GetDatabaseRuntimeName(LoadStage.AdjustRaw, namer);
            var toDb = table.GetDatabaseRuntimeName(LoadStage.AdjustStaging, namer);

            var fromTable = table.GetRuntimeName(LoadStage.AdjustRaw, namer);
            var toTable = table.GetRuntimeName(LoadStage.AdjustStaging, namer);

            var syntaxHelper = table.GetQuerySyntaxHelper();

            var fromCols = server.ExpectDatabase(fromDb).ExpectTable(fromTable).DiscoverColumns();
            var toCols = server.ExpectDatabase(toDb).ExpectTable(toTable).DiscoverColumns();

            //Migrate only columns that appear in both tables
            var commonColumns = fromCols.Select(f => f.GetRuntimeName()).Intersect(toCols.Select(t => t.GetRuntimeName())).ToArray();

            var sql = string.Format(@"INSERT INTO {1}({2}) SELECT DISTINCT {2} FROM {0}",
                syntaxHelper.EnsureFullyQualified(fromDb, null, fromTable),
                syntaxHelper.EnsureFullyQualified(toDb, null, toTable),
                string.Join(",", commonColumns.Select(c => syntaxHelper.EnsureWrapped(c))));

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to send SQL:" + sql));

            var cmd = server.GetCommand(sql, con);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Failed to migrate rows", ex));
                throw;
            }
        }

        sw.Stop();

        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Migrated all rows using INSERT INTO in " + sw.ElapsedMilliseconds + "ms"));
        return ExitCodeType.Success;
    }
}
