using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Applications.DicomDirectoryProcessor
{
    /// <summary>
    /// Command line program to process a directory and write an Accession
    /// Directory message to the message exchange for each directory found
    /// that contains DICOM (*.dcm) files.
    /// </summary>
    public static class DicomDirectoryProcessor
    {
        /// <summary>
        /// Main program.
        /// </summary>
        /// <param name="args">
        /// Arguments.  There should be exactly one argument that specified the
        /// path to the top level directory that is be searched.
        /// </param>
        /// <param name="fileSystem"></param>
        public static int Main(IEnumerable<string> args, IFileSystem? fileSystem = null)
        {
            int ret = SmiCliInit
                .ParseAndRun<DicomDirectoryProcessorCliOptions>(
                    args,
                    typeof(DicomDirectoryProcessor),
                    OnParse,
                    fileSystem ?? new FileSystem()
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, IFileSystem fileSystem, DicomDirectoryProcessorCliOptions parsedOptions)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomDirectoryProcessorHost(globals, parsedOptions, fileSystem));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
