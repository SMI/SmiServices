using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.MongoDBPopulator
{
    public static class MongoDBPopulator
    {
        /// <summary>
        /// Program entry point when run from command line
        /// </summary>
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit.ParseAndRun<CliOptions>(args, typeof(MongoDBPopulator), OnParse, fileSystem ?? new FileSystem());
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, CliOptions _)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new MongoDbPopulatorHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
