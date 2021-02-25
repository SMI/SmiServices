using Microservices.CohortExtractor.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.CohortExtractor
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, OnParse);
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
