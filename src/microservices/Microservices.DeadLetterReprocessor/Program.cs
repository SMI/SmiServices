
using CommandLine;
using Smi.Common.Execution;
using Smi.Common.Options;
using Microservices.DeadLetterReprocessor.Execution;
using Microservices.DeadLetterReprocessor.Options;

namespace Microservices.DeadLetterReprocessor
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<DeadLetterReprocessorCliOptions>(args)
                .MapResult(deadLetterCliOptions =>
                    {
                        GlobalOptions globals = GlobalOptions.Load(deadLetterCliOptions);

                        var bootstrapper = new MicroserviceHostBootstrapper(() => new DeadLetterReprocessorHost(globals, deadLetterCliOptions));
                        return bootstrapper.Main();
                    },
                    errs => -1
                );
        }
    }
}
