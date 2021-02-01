
using CommandLine;
using Smi.Common.Execution;
using Smi.Common.Options;
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
            return Parser.Default.ParseArguments<DicomTagReaderCliOptions>(args).MapResult(
                (o) =>
                {
                    GlobalOptions options = new GlobalOptionsFactory().Load(o);

                    if(o.File != null)
                    {
                        var host = new DicomTagReaderHost(options);
                        host.AccessionDirectoryMessageConsumer.RunSingleFile(o.File);
                        return 0;
                    }
                    else
                    {
                        // run in service mode
                        var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomTagReaderHost(options));

                        return bootstrapper.Main();
                    }

                },
                err => -100);
        }
    }
}
