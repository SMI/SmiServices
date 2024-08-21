using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.DicomReprocessor
{
    public static class DicomReprocessor
    {
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit.ParseAndRun<DicomReprocessorCliOptions>(args, typeof(DicomReprocessor), OnParse, fileSystem ?? new FileSystem());
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, DicomReprocessorCliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomReprocessorHost(globals, opts));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
