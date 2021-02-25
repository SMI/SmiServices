using Microservices.FileCopier.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.FileCopier
{
    internal static class Program
    {
        /// <summary>
        /// Program entry point when run from the command line
        /// </summary>
        /// <param name="args"></param>
        private static int Main(string[] args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, OnParse);
            return ret;
        }

        private static int OnParse(GlobalOptions globals, CliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new FileCopierHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
