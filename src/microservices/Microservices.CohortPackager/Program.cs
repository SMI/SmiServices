﻿using CommandLine;
using Microservices.CohortPackager.Execution;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Microservices.CohortPackager.Options;
using MongoDB.Driver;
using NLog;
using Smi.Common.Execution;
using Smi.Common.Helpers;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;
using System.IO;
using System.Reflection;

namespace Microservices.CohortPackager
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return
                Parser.Default.ParseArguments<CohortPackagerCliOptions>(args).MapResult((cohortPackagerCliOptions) =>
                {
                    GlobalOptions globalOptions = GlobalOptions.Load(cohortPackagerCliOptions);

                    if (cohortPackagerCliOptions.ExtractionId != default)
                        return RecreateReport(globalOptions, cohortPackagerCliOptions.ExtractionId);

                    var bootstrapper = new MicroserviceHostBootstrapper(() => new CohortPackagerHost(globalOptions));
                    return bootstrapper.Main();
                },
                err => 1);
        }

        private static int RecreateReport(GlobalOptions globalOptions, Guid jobId)
        {
            SetupLogging(globalOptions);
            Logger logger = LogManager.GetCurrentClassLogger();

            logger.Info($"Recreating report for job {jobId}");

            string procName = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new ApplicationException("Couldn't get the Assembly name!");
            MongoDbOptions mongoDbOptions = globalOptions.MongoDatabases.ExtractionStoreOptions;
            MongoClient client = MongoClientHelpers.GetMongoClient(mongoDbOptions, procName);
            var jobStore = new MongoExtractJobStore(client, mongoDbOptions.DatabaseName);

            string reportDir = Directory.GetCurrentDirectory();
            var reporter = new MicroserviceObjectFactory()
                .CreateInstance<IJobReporter>(
                    $"{typeof(IJobReporter).Namespace}.{globalOptions.CohortPackagerOptions.ReporterType}",
                    typeof(IJobReporter).Assembly, jobStore,
                    reportDir);

            try
            {
                reporter.CreateReport(jobId);
            }
            catch (Exception e)
            {
                logger.Error(e);
                return 1;
            }

            return 0;
        }

        // TODO(rkm 2020-07-23) This is the same as in MicroserviceHost -- pull out to a helper class
        private static void SetupLogging(GlobalOptions globalOptions)
        {
            string logConfigPath = !string.IsNullOrWhiteSpace(globalOptions.FileSystemOptions.LogConfigFile)
                ? globalOptions.FileSystemOptions.LogConfigFile
                : Path.Combine(globalOptions.CurrentDirectory, "Smi.NLog.config");

            if (!File.Exists(logConfigPath))
                throw new FileNotFoundException("Could not find the logging configuration in the current directory (Smi.NLog.config), or at the path specified by FileSystemOptions.LogConfigFile");

            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(logConfigPath, false);
        }
    }
}
