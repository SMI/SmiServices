using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;


namespace SmiServices.Applications.TriggerUpdates
{
    public static class TriggerUpdates
    {
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit
                .ParseAndRun(
                    args,
                    typeof(TriggerUpdates),
                    [
                        typeof(TriggerUpdatesFromMapperOptions),
                    ],
                    OnParse,
                    fileSystem ?? new FileSystem()
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, object opts)
        {
            var parsedOptions = SmiCliInit.Verify<TriggerUpdatesCliOptions>(opts);

            ITriggerUpdatesSource source = parsedOptions switch
            {
                TriggerUpdatesFromMapperOptions o => new MapperSource(globals, o),
                _ => throw new NotImplementedException($"No case for '{parsedOptions.GetType()}'")
            };

            var bootstrapper = new MicroserviceHostBootstrapper(() => new TriggerUpdatesHost(globals, source, messageBroker: null, fileSystem));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
