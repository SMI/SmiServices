using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;

namespace SmiServices.Microservices.CohortExtractor
{
    public static class Program
    {
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(Program), OnParse);
            return ret;
        }

        private static int OnParse(GlobalOptions globals, CliOptions opts)
        {
            //Use the auditor and request fullfilers specified in the yaml
            var bootstrapper = new MicroserviceHostBootstrapper(
                () => new CohortExtractorHost(
                    globals,
                    auditor: null,
                    fulfiller: null
                )
            );

            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
