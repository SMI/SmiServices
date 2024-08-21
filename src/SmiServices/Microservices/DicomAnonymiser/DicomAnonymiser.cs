using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.DicomAnonymiser
{
    public static class DicomAnonymiser
    {
        /// <summary>
        /// Program entry point when run from the command line
        /// </summary>
        /// <param name="args"></param>
        /// <param name="fileSystem"></param>
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(DicomAnonymiser), OnParse, fileSystem ?? new FileSystem());
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, CliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomAnonymiserHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
