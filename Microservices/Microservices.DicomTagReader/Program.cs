
using CommandLine;
using Microservices.Common.Execution;
using Microservices.Common.Options;
using Microservices.DicomTagReader.Execution;

namespace Microservices.DicomTagReader
{
    internal static class Program
    {
        /// <summary>
        /// Program entry point when run from the command line
        /// </summary>
        /// <param name="args"></param>
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CliOptions>(args).MapResult(
                (o) =>
                {
                    GlobalOptions options = GlobalOptions.Load(o);

                    var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomTagReaderHost(options));

                    return bootstrapper.Main();
                },
                err => -100);
        }
    }
}
