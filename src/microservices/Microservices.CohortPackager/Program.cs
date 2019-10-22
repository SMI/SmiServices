
using CommandLine;
using Microservices.CohortPackager.Execution;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Microservices.CohortPackager
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return
                Parser.Default.ParseArguments<CliOptions>(args).MapResult((o) =>
                {
                    var options = GlobalOptions.Load(o);
                    var bootstrapper = new MicroserviceHostBootstrapper(() => new CohortPackagerHost(options));
                    return bootstrapper.Main();
                },
                err => -100); //not parsed
        }
    }
}
