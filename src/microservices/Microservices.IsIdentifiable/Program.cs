using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Runners;
using Microservices.IsIdentifiable.Service;
using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microservices.IsIdentifiable
{
    public static class Program
    {
        public static int Main(IEnumerable<string> args)
        {
            int res = SmiCliInit.ParseAndRun(
                args,
                typeof(Program),
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
                IsIdentifiableMongoOptions o => Run(globals, o),
                IsIdentifiableServiceOptions o => Run(globals, o),
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

        private static int Run(GlobalOptions globals, IsIdentifiableMongoOptions opts)
        {
            var appId = $"{globals.HostProcessName}-{Process.GetCurrentProcess().Id}";

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

        private static int Run(GlobalOptions globals, IsIdentifiableServiceOptions serviceOpts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(
                () => new IsIdentifiableHost(globals, serviceOpts));
            return bootstrapper.Main();
        }
    }
}
