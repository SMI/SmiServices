using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;


namespace SmiServices.Microservices.UpdateValues
{
    public static class UpdateValues
    {
        public static int Main(IEnumerable<string> args)
        {
            return SmiCliInit
                .ParseAndRun<UpdateValuesCliOptions>(
                    args,
                    nameof(UpdateValues),
                    OnParse
                );
        }

        private static int OnParse(GlobalOptions globals, UpdateValuesCliOptions opts)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new UpdateValuesHost(globals));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
