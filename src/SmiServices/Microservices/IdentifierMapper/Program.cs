using Smi.Common.Execution;
using Smi.Common.Options;
using System.Collections.Generic;

namespace SmiServices.Microservices.IdentifierMapper
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
            var bootstrapper = new MicroserviceHostBootstrapper(() => new IdentifierMapperHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
