using Applications.DicomDirectoryProcessor.Execution;
using Applications.DicomDirectoryProcessor.Options;
using Smi.Common.Execution;
using Smi.Common.Options;
using System.Collections.Generic;

namespace Applications.DicomDirectoryProcessor
{
    /// <summary>
    /// Command line program to process a directory and write an Accession
    /// Directory message to the message exchange for each directory found
    /// that contains DICOM (*.dcm) files.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main program.
        /// </summary>
        /// <param name="args">
        /// Arguments.  There should be exactly one argument that specified the
        /// path to the top level directory that is be searched.
        /// </param>
        public static int Main(IEnumerable<string> args)
        {
            int ret = SmiCliInit
                .ParseAndRun<DicomDirectoryProcessorCliOptions>(
                    args,
                    typeof(Program),
                    OnParse
                );
            return ret;
        }

        private static int OnParse(GlobalOptions globals, DicomDirectoryProcessorCliOptions parsedOptions)
        {
            var bootstrapper = new MicroserviceHostBootstrapper(() => new DicomDirectoryProcessorHost(globals, parsedOptions));
            int ret = bootstrapper.Main();
            return ret;
        }
    }
}
