using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;

namespace SmiServices.Microservices.FileCopier
{
    public static class FileCopier
    {
        /// <summary>
        /// Program entry point when run from the command line
        /// </summary>
        /// <param name="args"></param>
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(FileCopier), OnParse);
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
