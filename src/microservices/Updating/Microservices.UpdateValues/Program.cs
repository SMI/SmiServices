using Microservices.UpdateValues.Execution;
using Microservices.UpdateValues.Options;
using Smi.Common.Execution;
using Smi.Common.Options;


namespace Microservices.UpdateValues
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return SmiCliInit
                .ParseAndRun<UpdateValuesCliOptions>(
                    args,
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
