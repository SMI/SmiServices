
using CommandLine;
using Microservices.Common.Execution;
using Microservices.Common.Options;
using Microservices.DicomRelationalMapper.Execution;

namespace Microservices.DicomRelationalMapper
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CliOptions>(args).MapResult((o) =>
            {
                GlobalOptions options = GlobalOptions.Load(o);

                var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomRelationalMapperHost(options));
                return bootstrapper.Main();

            }, err => -100);
        }
    }
}
