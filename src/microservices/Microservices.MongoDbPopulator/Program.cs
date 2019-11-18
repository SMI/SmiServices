
using CommandLine;
using Microservices.MongoDBPopulator.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.MongoDBPopulator
{
    internal static class Program
    {
        /// <summary>
        /// Program entry point when run from command line
        /// </summary>
        private static int Main(string[] args)
        {
            return
                Parser.Default.ParseArguments<CliOptions>(args).MapResult((o) =>
                {
                    GlobalOptions options = GlobalOptions.Load(o);

                    var bootStrapper = new MicroserviceHostBootstrapper(() => new MongoDbPopulatorHost(options));
                    return bootStrapper.Main();
                },
                err => -100);
        }
    }
}
