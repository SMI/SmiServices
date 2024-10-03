using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace SmiServices.Applications.ExtractImages
{
    public static class ExtractImages
    {
        [ExcludeFromCodeCoverage]
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit
                .ParseAndRun<ExtractImagesCliOptions>(
                    args,
                    nameof(ExtractImages),
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
