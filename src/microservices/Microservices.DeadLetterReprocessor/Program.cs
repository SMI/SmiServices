using Microservices.DeadLetterReprocessor.Execution;
using Microservices.DeadLetterReprocessor.Options;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.DeadLetterReprocessor
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int ret = SmiCliInit.ParseAndRun<DeadLetterReprocessorCliOptions>(args, OnParse);
            return ret;
        }

        private static int OnParse(GlobalOptions globals, DeadLetterReprocessorCliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DeadLetterReprocessorHost(globals, opts));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
