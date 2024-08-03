using Microservices.UpdateValues.Execution;
using Microservices.UpdateValues.Options;
using Smi.Common.Execution;
using Smi.Common.Options;
using System.Collections.Generic;


namespace Microservices.UpdateValues
{
    public static class Program
    {
        public static int Main(IEnumerable<string> args)
        {
            return SmiCliInit
                .ParseAndRun<UpdateValuesCliOptions>(
                    args,
                    typeof(Program),
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
