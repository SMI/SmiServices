using Microservices.CohortPackager.Execution;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.CohortPackager.Options;
using MongoDB.Driver;
using NLog;
using Smi.Common.Execution;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;


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
            MongoClient client = MongoClientHelpers.GetMongoClient(mongoDbOptions, globalOptions.HostProcessName);
            var jobStore = new MongoExtractJobStore(client, mongoDbOptions.DatabaseName);

            string newLine = Regex.Unescape(cliOptions.OutputNewLine ?? globalOptions.CohortPackagerOptions.ReportNewLine);

            // NOTE(rkm 2020-10-22) Sets the extraction root to the current directory
            var reporter = new JobReporter(
                jobStore,
                new FileSystem(),
                Directory.GetCurrentDirectory(),
                newLine
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
}
