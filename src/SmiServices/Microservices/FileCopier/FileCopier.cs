using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.FileCopier
{
    public static class FileCopier
    {
        /// <summary>
        /// Program entry point when run from the command line
        /// </summary>
        /// <param name="args"></param>
        /// <param name="fileSystem"></param>
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(FileCopier), OnParse, fileSystem ?? new FileSystem());
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, CliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new FileCopierHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
