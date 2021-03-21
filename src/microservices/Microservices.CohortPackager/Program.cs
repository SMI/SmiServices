using Microservices.CohortPackager.Execution;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.CohortPackager.Options;
using MongoDB.Driver;
using NLog;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;


namespace Microservices.CohortPackager
{
    public static class Program
    {
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit.ParseAndRun<CohortPackagerCliOptions>(args, typeof(Program), OnParse);
            return ret;
        }

        private static int OnParse(GlobalOptions globals, CohortPackagerCliOptions opts)
        {
            if (opts.ExtractionId != default)
                return RecreateReport(globals, opts);

            var bootstrapper = new MicroserviceHostBootstrapper(() => new CohortPackagerHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }

        private static int RecreateReport(GlobalOptions globalOptions, CohortPackagerCliOptions cliOptions)
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            logger.Info($"Recreating report for job {cliOptions.ExtractionId}");

            MongoDbOptions mongoDbOptions = globalOptions.MongoDatabases.ExtractionStoreOptions;
            MongoClient client = MongoClientHelpers.GetMongoClient(mongoDbOptions, SmiLogging.HostProcessName);
            var jobStore = new MongoExtractJobStore(client, mongoDbOptions.DatabaseName);

            // NOTE(rkm 2020-10-22) Sets the extraction root to the current directory
            IJobReporter reporter = JobReporterFactory.GetReporter(
                "FileReporter",
                jobStore,
                new FileSystem(),
                Directory.GetCurrentDirectory(),
                cliOptions.ReportFormat.ToString(),
                cliOptions.OutputNewLine ?? globalOptions.CohortPackagerOptions.ReportNewLine,
                createJobIdFile: false
            );

            try
            {
                reporter.CreateReport(cliOptions.ExtractionId);
            }
            catch (Exception e)
            {
                logger.Error(e);
                return 1;
            }

            return 0;
        }
    }
}
