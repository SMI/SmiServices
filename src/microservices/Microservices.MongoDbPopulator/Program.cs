using Microservices.MongoDBPopulator.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.MongoDBPopulator
{
    internal static class Program
    {
        /// <summary>
        /// Program entry point when run from command line
        /// </summary>
        private static int Main(string[] args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, OnParse);
            return ret;
        }

        private static int OnParse(GlobalOptions globals, CliOptions _)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new MongoDbPopulatorHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
