
using Applications.DicomDirectoryProcessor.Execution;
using Applications.DicomDirectoryProcessor.Options;
using CommandLine;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Applications.DicomDirectoryProcessor
{
    /// <summary>
    /// Command line program to process a directory and write an Accession
    /// Directory message to the message exchange for each directory found
    /// that contains DICOM (*.dcm) files.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Main program.
        /// </summary>
        /// <param name="args">
        /// Arguments.  There should be exactly one argument that specified the
        /// path to the top level directory that is be searched.
        /// </param>
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<DicomDirectoryProcessorCliOptions>(args).MapResult(
                processDirectoryOptions =>
                {
                    GlobalOptions globalOptions = GlobalOptions.Load(processDirectoryOptions);

                    var bootStrapper = new MicroserviceHostBootstrapper(() => new DicomDirectoryProcessorHost(globalOptions, processDirectoryOptions));
                    return bootStrapper.Main();
                },
                errs => -100);
        }
    }
}
