using CommandLine;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using Smi.Common.Execution;
using Smi.Common.Options;
using System;
using TriggerUpdates.Execution;

namespace TriggerUpdates
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<TriggerUpdatesFromMapperOptions,TriggerUpdatesFromMongo>(args)
                .MapResult(
                (TriggerUpdatesFromMapperOptions opts) => Run(opts,(g)=>new MapperSource(g,opts)),
                (TriggerUpdatesFromMongo opts) => Run(opts,(g)=>new MongoSource(g,opts)),
                errs => -100);
        }

        private static int Run(TriggerUpdatesCliOptions opts, Func<GlobalOptions,ITriggerUpdatesSource> sourceFactory)
        {
            GlobalOptions globalOptions = new GlobalOptionsFactory().Load(opts);

            var bootStrapper = new MicroserviceHostBootstrapper(() => new TriggerUpdatesHost(globalOptions, sourceFactory(globalOptions)));
            return bootStrapper.Main();
        }
    }
}
