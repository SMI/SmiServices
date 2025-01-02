using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.CohortExtractor;

public static class CohortExtractor
{
    [ExcludeFromCodeCoverage]
    public static int Main(IEnumerable<string> args)
    {
        int ret = SmiCliInit.ParseAndRun<CliOptions>(args, nameof(CohortExtractor), OnParse);
        return ret;
    }

    private static int OnParse(GlobalOptions globals, CliOptions opts)
    {
        //Use the auditor and request fullfilers specified in the yaml
        var bootstrapper = new MicroserviceHostBootstrapper(
            () => new CohortExtractorHost(
                globals,
                fulfiller: null
            )
        );

        int ret = bootstrapper.Main();
        return ret;
    }
}
