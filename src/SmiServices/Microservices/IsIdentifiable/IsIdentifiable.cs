using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;

namespace SmiServices.Microservices.IsIdentifiable
{
    public static class IsIdentifiable
    {
        public static int Main(IEnumerable<string> args)
        {

            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(IsIdentifiable), OnParse);
            return ret;
        }
        private static int OnParse(GlobalOptions globals, CliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(
                () => new IsIdentifiableHost(globals));
            return bootstrapper.Main();
        }

    }
}
