using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Smi.Common.Execution;
using Smi.Common.Options;


namespace Applications.ExtractImages
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit
                .ParseAndRun<ExtractImagesCliOptions>(
                    args,
                    typeof(Program),
                    OnParse
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, ExtractImagesCliOptions parsedOptions)
        {
            var bootstrapper =
                new MicroserviceHostBootstrapper(() => new ExtractImagesHost(globals, parsedOptions));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
