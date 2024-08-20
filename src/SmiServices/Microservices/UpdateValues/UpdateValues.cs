using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;


namespace SmiServices.Microservices.UpdateValues
{
    public static class UpdateValues
    {
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            return SmiCliInit
                .ParseAndRun<UpdateValuesCliOptions>(
                    args,
                    typeof(UpdateValues),
                    OnParse,
                    fileSystem ?? new FileSystem()
                );
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, UpdateValuesCliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new UpdateValuesHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
