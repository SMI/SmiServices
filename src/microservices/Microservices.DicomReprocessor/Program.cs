using Microservices.DicomReprocessor.Execution;
using Microservices.DicomReprocessor.Options;
using Smi.Common.Execution;
using Smi.Common.Options;

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
            int ret = SmiCliInit.ParseAndRun<DicomReprocessorCliOptions>(args, OnParse);
            return ret;
        }

        private static int OnParse(GlobalOptions globals, DicomReprocessorCliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomReprocessorHost(globals, opts));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
