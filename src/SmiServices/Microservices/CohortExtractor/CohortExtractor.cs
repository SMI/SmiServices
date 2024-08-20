using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.CohortExtractor
{
    public static class CohortExtractor
    {
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(CohortExtractor), OnParse, fileSystem ?? new FileSystem());
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem? fileSystem, CliOptions opts)
        {
            //Use the auditor and request fullfilers specified in the yaml
            var bootstrapper = new MicroserviceHostBootstrapper(
                () => new CohortExtractorHost(
                    globals,
                    auditor: null,
                    fulfiller: null
                )
            );

            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
