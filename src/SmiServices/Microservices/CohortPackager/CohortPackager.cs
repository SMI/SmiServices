using MongoDB.Driver;
using NLog;
using SmiServices.Common.Execution;
using SmiServices.Common.MongoDB;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage.MongoDB;
using SmiServices.Microservices.CohortPackager.JobProcessing.Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Diagnostics.CodeAnalysis;


namespace SmiServices.Microservices.CohortPackager;

public static class CohortPackager
{
    [ExcludeFromCodeCoverage]
    public static int Main(IEnumerable<string> args)
    {
        int ret = SmiCliInit.ParseAndRun<CohortPackagerCliOptions>(args, nameof(CohortPackager), OnParse);
        return ret;
    }

    private static int OnParse(GlobalOptions globals, CohortPackagerCliOptions opts)
    {
        if (opts.ExtractionId != default)
            return RecreateReports(globals, opts);

        var bootstrapper = new MicroserviceHostBootstrapper(() => new CohortPackagerHost(globals));
        int ret = bootstrapper.Main();
        return ret;
    }

    private static int RecreateReports(GlobalOptions globalOptions, CohortPackagerCliOptions cliOptions)
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        var mongoDbOptions = globalOptions.MongoDatabases?.ExtractionStoreOptions;
        if (mongoDbOptions == null)
        {
            logger.Error($"{nameof(MongoDatabases.ExtractionStoreOptions)} must be set");
            return 1;
        }

        var databaseName = mongoDbOptions.DatabaseName;
        if (databaseName == null)
        {
            logger.Error($"{nameof(mongoDbOptions.DatabaseName)} must be set");
            return 1;
        }

        logger.Info($"Recreating report for job {cliOptions.ExtractionId}");

        MongoClient client = MongoClientHelpers.GetMongoClient(mongoDbOptions, globalOptions.HostProcessName);
        var jobStore = new MongoExtractJobStore(client, databaseName);

        // NOTE(rkm 2020-10-22) Sets the extraction root to the current directory
        var reporter = new JobReporter(
            jobStore,
            new FileSystem(),
            Directory.GetCurrentDirectory(),
            cliOptions.OutputNewLine ?? globalOptions.CohortPackagerOptions?.ReportNewLine
        );

        try
        {
            reporter.CreateReports(cliOptions.ExtractionId);
        }
        catch (Exception e)
        {
            logger.Error(e);
            return 1;
        }

        return 0;
    }
}
