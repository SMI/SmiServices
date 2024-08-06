using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;


namespace SmiServices.Applications.TriggerUpdates
{
    public static class TriggerUpdates
    {
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit
                .ParseAndRun(
                    args,
                    typeof(TriggerUpdates),
                    new[]
                    {
                        typeof(TriggerUpdatesFromMapperOptions),
                    },
                    OnParse
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, object opts)
        {
            var parsedOptions = SmiCliInit.Verify<TriggerUpdatesCliOptions>(opts);

            ITriggerUpdatesSource source = parsedOptions switch
            {
                TriggerUpdatesFromMapperOptions o => new MapperSource(globals, o),
                _ => throw new NotImplementedException($"No case for '{parsedOptions.GetType()}'")
            };

            var bootstrapper = new MicroserviceHostBootstrapper(() => new TriggerUpdatesHost(globals, source));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
