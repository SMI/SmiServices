using CommandLine;
using Microservices.UpdateValues.Execution;
using Microservices.UpdateValues.Options;
using Smi.Common.Execution;
using Smi.Common.Options;
using System;

namespace Microservices.UpdateValues
{
    class Program
    {
        static int Main(string[] args)
        {
            
            return Parser.Default.ParseArguments<UpdateValuesCliOptions>(args).MapResult(
                opts =>
                {
                    GlobalOptions globalOptions = new GlobalOptionsFactory().Load(opts);

                    var bootStrapper = new MicroserviceHostBootstrapper(() => new UpdateValuesHost(globalOptions));
                    return bootStrapper.Main();
                },
                errs => -100);
        }
    }
}
