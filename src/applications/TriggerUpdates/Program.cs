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
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<OracleImplementation>();
            ImplementationManager.Load<MicrosoftSQLImplementation>();
            ImplementationManager.Load<PostgreSqlImplementation>();
            
            return Parser.Default.ParseArguments<TriggerUpdatesFromMapperOptions,TriggerUpdatesFromMongo>(args)
                .MapResult(
                (TriggerUpdatesFromMapperOptions opts) => Run(opts,new MapperSource(opts)),
                (TriggerUpdatesFromMongo opts) => Run(opts,new MongoSource(opts)),
                errs => -100);
        }

        private static int Run(TriggerUpdatesCliOptions opts, ITriggerUpdatesSource source)
        {
            
            GlobalOptions globalOptions = new GlobalOptionsFactory().Load(opts);

            var bootStrapper = new MicroserviceHostBootstrapper(() => new TriggerUpdatesHost(globalOptions, source));
            return bootStrapper.Main();

        }
    }
}
