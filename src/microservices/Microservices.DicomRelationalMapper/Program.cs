using Microservices.DicomRelationalMapper.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.DicomRelationalMapper
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
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomRelationalMapperHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
