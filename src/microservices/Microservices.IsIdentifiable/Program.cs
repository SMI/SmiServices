using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommandLine;
using DicomTypeTranslation.Helpers;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Runners;
using Microservices.IsIdentifiable.Service;
using NLog;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.IsIdentifiable
{
    internal static class Program
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private static readonly Process _process = Process.GetCurrentProcess();

        public static int Main(string[] args)
        {
            if (!DicomDatasetHelpers.CorrectFoDicomVersion())
            {
                Console.WriteLine("Incorrect fo-dicom version for the current platform");
                return 1;
            }

            ImplementationManager.Load<MicrosoftSQLImplementation>();
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<OracleImplementation>();
            ImplementationManager.Load<PostgreSqlImplementation>();

            //If running as a self contained micro service (getting messages from RabbitMQ)
            if (args.Length == 1 && string.Equals(args[0], "--service", StringComparison.CurrentCultureIgnoreCase))
            {
                var options = GlobalOptions.Load();
                
                var bootstrapper = new MicroserviceHostBootstrapper(
                    () => new IsIdentifiableHost(options));
                return bootstrapper.Main();
            }

            try
            {
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (assemblyFolder == null)
                    throw new Exception("Could not get the assemblyFolder");

                string location = Path.Combine(assemblyFolder, "Smi.NLog.config");

                if (!File.Exists(location))
                {
                    Console.WriteLine("File '" + location + "' did not exist");
                    return -5;
                }

                var config = new NLog.Config.XmlLoggingConfiguration(location, false);
                LogManager.Configuration = config;

                return GetParser()
                    .ParseArguments<IsIdentifiableRelationalDatabaseOptions, IsIdentifiableDicomFileOptions, IsIdentifiableMongoOptions>(args)
                    .MapResult(
                        //Add new verbs as options here and invoke relevant runner
                        (IsIdentifiableRelationalDatabaseOptions opts) => Run(opts),
                        (IsIdentifiableDicomFileOptions opts) => Run(opts),
                        (IsIdentifiableMongoOptions opts) => Run(opts),
                        HandleErrors);

            }
            catch (Exception e)
            {
                _logger.Error(e);
                return 1;
            }
        }

        private static int HandleErrors(IEnumerable<Error> errs)
        {
            foreach (Error err in errs)
                _logger.Error(err);

            return 1;
        }

        private static int Run(IsIdentifiableDicomFileOptions opts)
        {
            using(var runner = new DicomFileRunner(opts))
                return runner.Run();
        }

        private static int Run(IsIdentifiableRelationalDatabaseOptions opts)
        {
            using(var runner = new DatabaseRunner(opts))
                return runner.Run();
        }

        private static int Run(IsIdentifiableMongoOptions opts)
        {
            string appId = _process.ProcessName + "-" + _process.Id;
            using (var runner = new IsIdentifiableMongoRunner(opts, appId))
            {
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    runner.Stop();
                };

                return runner.Run();
            }
        }

        private static Parser GetParser()
        {
            var defaults = Parser.Default.Settings;

            var parser = new Parser(settings =>
            {
                settings.CaseSensitive = false;
                settings.CaseInsensitiveEnumValues = true;
                settings.EnableDashDash = defaults.EnableDashDash;
                settings.HelpWriter = defaults.HelpWriter;
                settings.IgnoreUnknownArguments = defaults.IgnoreUnknownArguments;
                settings.MaximumDisplayWidth = defaults.MaximumDisplayWidth;
                settings.ParsingCulture = defaults.ParsingCulture;
            });

            return parser;
        }

    }
}
