using SmiServices.Applications.DicomDirectoryProcessor.DirectoryFinders;
using SmiServices.Common.Execution;
using SmiServices.Common.Options;
using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;

namespace SmiServices.Applications.DicomDirectoryProcessor
{
    /// <summary>
    /// Processes directories to find those that contain DICOM files
    /// </summary>
    public class DicomDirectoryProcessorHost : MicroserviceHost
    {
        private readonly DicomDirectoryProcessorCliOptions _cliOptions;
        private readonly IDicomDirectoryFinder _ddf;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cliOptions">Common microservices options.  Must contain details for an message exchange labelled as "accessionDirectories"</param>
        /// <param name="fileSystem"></param>
        /// <param name="globals">Configuration settings for the program</param>
        public DicomDirectoryProcessorHost(GlobalOptions globals, DicomDirectoryProcessorCliOptions cliOptions, IFileSystem? fileSystem = null)
            : base(globals, fileSystem ?? new FileSystem())
        {
            _cliOptions = cliOptions;

            IDirectoryInfo toProcessDir = FileSystem.DirectoryInfo.New(cliOptions.ToProcess);

            if (!cliOptions.DirectoryFormat!.ToLower().Equals("list"))
            {
                // TODO(rkm 2020-02-12) I think we want to check this regardless of the mode
                // (bp 2020-02-13) By not doing this check on list means that the list of paths is not required to be in PACS and can be imported from anywhere
                if (!FileSystem.Directory.Exists(globals.FileSystemOptions!.FileSystemRoot))
                    throw new ArgumentException($"Cannot find the FileSystemRoot specified in the given MicroservicesOptions ({globals.FileSystemOptions.FileSystemRoot})");

                if (!toProcessDir.Exists)
                    throw new ArgumentException($"Could not find directory {toProcessDir.FullName}");

                if (!toProcessDir.FullName.StartsWith(globals.FileSystemOptions.FileSystemRoot, true, CultureInfo.CurrentCulture))
                    throw new ArgumentException($"Directory parameter ({toProcessDir.FullName}) must be below the FileSystemRoot ({globals.FileSystemOptions.FileSystemRoot})");
            }
            else
            {
                if (!FileSystem.File.Exists(toProcessDir.FullName))
                    throw new ArgumentException($"Could not find accession directory list file ({toProcessDir.FullName})");

                if (!FileSystem.Path.GetExtension(toProcessDir.FullName).Equals(".csv"))
                    throw new ArgumentException($"When in 'list' mode, path to accession directory file of format .csv expected ({toProcessDir.FullName})");
            }

            switch (cliOptions.DirectoryFormat.ToLower())
            {
                case "pacs":
                    Logger.Info("Creating PACS directory finder");

                    _ddf = new PacsDirectoryFinder(globals.FileSystemOptions!.FileSystemRoot!,
                        globals.FileSystemOptions.DicomSearchPattern!, MessageBroker.SetupProducer(globals.ProcessDirectoryOptions!.AccessionDirectoryProducerOptions!, isBatch: false));
                    break;
                case "list":
                    Logger.Info("Creating accession directory lister");

                    _ddf = new AccessionDirectoryLister(globals.FileSystemOptions!.FileSystemRoot!,
                        globals.FileSystemOptions.DicomSearchPattern!, MessageBroker.SetupProducer(globals.ProcessDirectoryOptions!.AccessionDirectoryProducerOptions!, isBatch: false));
                    break;
                case "default":
                    Logger.Info("Creating basic directory finder");

                    _ddf = new BasicDicomDirectoryFinder(globals.FileSystemOptions!.FileSystemRoot!,
                        globals.FileSystemOptions.DicomSearchPattern!, MessageBroker.SetupProducer(globals.ProcessDirectoryOptions!.AccessionDirectoryProducerOptions!, isBatch: false));
                    break;
                case "zips":
                    Logger.Info("Creating zip directory finder");

                    _ddf = new ZipDicomDirectoryFinder(globals.FileSystemOptions!.FileSystemRoot!,
                        globals.FileSystemOptions.DicomSearchPattern!, MessageBroker.SetupProducer(globals.ProcessDirectoryOptions!.AccessionDirectoryProducerOptions!, isBatch: false));
                    break;
                default:
                    throw new ArgumentException(
                        $"Could not match directory format {cliOptions.DirectoryFormat} to an directory scan implementation");
            }
        }

        /// <summary>
        /// Searches from the given directory to look for DICOM files and writes AccessionDirectoryMessages to the message exchange
        /// </summary>
        public override void Start()
        {
            IDirectoryInfo toProcessDir = FileSystem.DirectoryInfo.New(_cliOptions.ToProcess);

            try
            {
                _ddf.SearchForDicomDirectories(toProcessDir.FullName);
            }
            catch (Exception e)
            {
                Fatal(e.Message, e);
                return;
            }

            Stop("Directory scan completed");
        }

        public override void Stop(string reason)
        {
            _ddf.Stop();
            base.Stop(reason);
        }
    }
}
