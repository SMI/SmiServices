using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Applications.DicomDirectoryProcessor;

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
    [ExcludeFromCodeCoverage]
    public static int Main(IEnumerable<string> args)
    {
        int ret = SmiCliInit
            .ParseAndRun<DicomDirectoryProcessorCliOptions>(
                args,
                nameof(DicomDirectoryProcessor),
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
