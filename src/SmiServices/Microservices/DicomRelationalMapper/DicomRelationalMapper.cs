using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;

namespace SmiServices.Microservices.DicomRelationalMapper
{
    public static class DicomRelationalMapper
    {
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, nameof(DicomRelationalMapper), OnParse);
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
