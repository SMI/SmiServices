using CommandLine;
using Smi.Common.Options;


namespace Microservices.FileCopier
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

                    //var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomTagReaderHost(options));
                    //return bootstrapper.Main();
                    return 0;
                },
                err => -100);
        }
    }
}
