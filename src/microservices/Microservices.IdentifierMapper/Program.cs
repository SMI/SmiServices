using Microservices.IdentifierMapper.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.IdentifierMapper
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, OnParse);
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
