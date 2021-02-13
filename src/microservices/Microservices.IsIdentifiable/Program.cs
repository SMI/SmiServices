using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Runners;
using Microservices.IsIdentifiable.Service;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Options;
using System;
using System.Diagnostics;

namespace Microservices.IsIdentifiable
{
    internal static class Program
    {
        private static readonly Process _process = Process.GetCurrentProcess();

        public static int Main(string[] args)
        {
            int res = SmiCliInit.ParseAndRun(
                args,
                new[]
                {
                    typeof(IsIdentifiableRelationalDatabaseOptions),
                    typeof(IsIdentifiableDicomFileOptions),
                    typeof(IsIdentifiableMongoOptions),
                    typeof(IsIdentifiableServiceOptions),
                    typeof(IsIdentifiableFileOptions),
                },
                OnParse
            );
            return res;
        }

        private static int OnParse(GlobalOptions globals, object parsedOpts)
        {
            var opts = SmiCliInit.Verify<IsIdentifiableAbstractOptions>(parsedOpts);

            return opts switch
            {
                IsIdentifiableRelationalDatabaseOptions o => Run(o),
                IsIdentifiableDicomFileOptions o => Run(o),
                IsIdentifiableMongoOptions o => Run(o),
                IsIdentifiableServiceOptions o => Run(o),
                IsIdentifiableFileOptions o => Run(o),
                _ => throw new NotImplementedException($"No case for '{opts.GetType()}'")
            };
        }

        private static int Run(IsIdentifiableDicomFileOptions opts)
        {
            using (var runner = new DicomFileRunner(opts))
                return runner.Run();
        }

        private static int Run(IsIdentifiableRelationalDatabaseOptions opts)
        {
            FansiImplementations.Load();

            using (var runner = new DatabaseRunner(opts))
                return runner.Run();
        }
        private static int Run(IsIdentifiableFileOptions opts)
        {
            using (var runner = new FileRunner(opts))
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

        private static int Run(IsIdentifiableServiceOptions serviceOpts)
        {
            var options = new GlobalOptionsFactory().Load(serviceOpts.YamlFile);

            var bootstrapper = new MicroserviceHostBootstrapper(
                () => new IsIdentifiableHost(options, serviceOpts));
            return bootstrapper.Main();
        }
    }
}
