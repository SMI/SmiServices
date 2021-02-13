﻿using Smi.Common.Execution;
using Smi.Common.Options;
using System;
using Applications.TriggerUpdates.Execution;
using Applications.TriggerUpdates.Options;
using Smi.Common;


namespace Applications.TriggerUpdates
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            int ret = SmiCliInit
                .ParseAndRun(
                    args,
                    new[]
                    {
                        typeof(TriggerUpdatesFromMapperOptions),
                        typeof(TriggerUpdatesFromMongoOptions)
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
                TriggerUpdatesFromMongoOptions o => new MongoSource(globals, o),
                _ => throw new NotImplementedException($"No case for '{parsedOptions.GetType()}'")
            };

            var bootstrapper = new MicroserviceHostBootstrapper(() => new TriggerUpdatesHost(globals, source));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
