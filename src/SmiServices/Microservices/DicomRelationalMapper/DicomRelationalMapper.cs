using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.DicomRelationalMapper
{
    public static class DicomRelationalMapper
    {
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(DicomRelationalMapper), OnParse, fileSystem ?? new FileSystem());
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, CliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomRelationalMapperHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
