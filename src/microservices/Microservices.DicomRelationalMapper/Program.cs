
using CommandLine;
using Smi.Common.Execution;
using Smi.Common.Options;
using Microservices.DicomRelationalMapper.Execution;

namespace Microservices.DicomRelationalMapper
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CliOptions>(args).MapResult((o) =>
            {
                GlobalOptions options = new GlobalOptionsFactory().Load(o);

                var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomRelationalMapperHost(options));
                return bootstrapper.Main();

            }, err => -100);
        }
    }
}
