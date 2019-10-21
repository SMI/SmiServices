
using CommandLine;
using Microservices.Common.Execution;
using Microservices.Common.Options;
using Microservices.DicomReprocessor.Execution;
using Microservices.DicomReprocessor.Options;

namespace Microservices.DicomReprocessor
{
    /// <summary>
    /// Command line program to reprocess documents from MongoDb
    /// and push them back onto the IdentifiableImageQueue for
    /// reprocessing into the relational database.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<DicomReprocessorCliOptions>(args)
                .MapResult(dicomReprocessorCliOptions =>
                {
                    GlobalOptions options = GlobalOptions.Load(dicomReprocessorCliOptions);

                    var bootStrapper = new MicroserviceHostBootstrapper(() => new DicomReprocessorHost(options, dicomReprocessorCliOptions));
                    return bootStrapper.Main();

                }, err => -100);
        }
    }
}
