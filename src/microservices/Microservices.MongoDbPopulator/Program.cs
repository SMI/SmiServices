using Microservices.MongoDBPopulator.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;
using System.Collections.Generic;

namespace Microservices.MongoDBPopulator
{
    public static class Program
    {
        /// <summary>
        /// Program entry point when run from command line
        /// </summary>
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(Program), OnParse);
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
