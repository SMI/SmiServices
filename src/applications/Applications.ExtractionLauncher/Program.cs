using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Smi.Common.Execution;
using Smi.Common.Options;


namespace Applications.ExtractionLauncher
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit
                .ParseAndRun<ExtractionLauncherCliOptions>(
                    args,
                    typeof(Program),
                    OnParse
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, ExtractionLauncherCliOptions parsedOptions)
        {
            var bootstrapper =
                new MicroserviceHostBootstrapper(() => new ExtractionLauncherHost(globals, parsedOptions));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}