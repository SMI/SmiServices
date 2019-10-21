
using CommandLine;
using Microservices.Common.Execution;
using Microservices.Common.Options;
using Microservices.IdentifierMapper.Execution;

namespace Microservices.IdentifierMapper
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CliOptions>(args).MapResult(
                cliOptions =>
                    {
                        GlobalOptions options = GlobalOptions.Load(cliOptions);

                        var bootstrapper = new MicroserviceHostBootstrapper(() => new IdentifierMapperHost(options));
                        return bootstrapper.Main();
                    },
                err => -100);
        }
    }
}
