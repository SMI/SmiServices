using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.IsIdentifiable
{
    public static class IsIdentifiable
    {
        [ExcludeFromCodeCoverage]
        public static int Main(IEnumerable<string> args)
        {

            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, nameof(IsIdentifiable), OnParse);
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
