using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;


namespace SmiServices.Applications.ExtractImages
{
    [ExcludeFromCodeCoverage]
    public static class ExtractImages
    {
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit
                .ParseAndRun<ExtractImagesCliOptions>(
                    args,
                    typeof(ExtractImages),
                    OnParse,
                    fileSystem ?? new FileSystem()
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, ExtractImagesCliOptions parsedOptions)
        {
            var bootstrapper =
                new MicroserviceHostBootstrapper(() => new ExtractImagesHost(globals, parsedOptions, fileSystem));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
