using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.MongoDBPopulator;

public static class MongoDBPopulator
{
    /// <summary>
    /// Program entry point when run from command line
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static int Main(IEnumerable<string> args)
    {
        int ret = SmiCliInit.ParseAndRun<CliOptions>(args, nameof(MongoDBPopulator), OnParse);
        return ret;
    }

    private static int OnParse(GlobalOptions globals, CliOptions _)
    {
        var bootstrapper = new MicroserviceHostBootstrapper(() => new MongoDbPopulatorHost(globals));
        int ret = bootstrapper.Main();
        return ret;
    }
}
