﻿
using System;
using System.Globalization;
using System.IO;
using Applications.DicomDirectoryProcessor.Execution.DirectoryFinders;
using Applications.DicomDirectoryProcessor.Options;
using Smi.Common.Execution;
using Smi.Common.Options;

namespace Applications.DicomDirectoryProcessor.Execution
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
        /// <param name="globals">Configuration settings for the program</param>
        public DicomDirectoryProcessorHost(GlobalOptions globals, DicomDirectoryProcessorCliOptions cliOptions)
            : base(globals)
        {
            _cliOptions = cliOptions;

            if (!cliOptions.DirectoryFormat.ToLower().Equals("list"))
            {
                // TODO(rkm 2020-02-12) I think we want to check this regardless of the mode
                // (bp 2020-02-13) By not doing this check on list means that the list of paths is not required to be in PACS and can be imported from anywhere
                if (!Directory.Exists(globals.FileSystemOptions.FileSystemRoot))
                    throw new ArgumentException("Cannot find the FileSystemRoot specified in the given MicroservicesOptions (" + globals.FileSystemOptions.FileSystemRoot + ")");

                if (!cliOptions.ToProcessDir.Exists)
                    throw new ArgumentException("Could not find directory " + cliOptions.ToProcessDir.FullName);

                if (!cliOptions.ToProcessDir.FullName.StartsWith(globals.FileSystemOptions.FileSystemRoot, true, CultureInfo.CurrentCulture))
                    throw new ArgumentException("Directory parameter (" + cliOptions.ToProcessDir.FullName + ") must be below the FileSystemRoot (" + globals.FileSystemOptions.FileSystemRoot + ")");
            }
            else
            {
                if (!File.Exists(cliOptions.ToProcessDir.FullName))
                    throw new ArgumentException("Could not find accession directory list file (" + cliOptions.ToProcessDir.FullName + ")");

                if (!Path.GetExtension(cliOptions.ToProcessDir.FullName).Equals(".csv"))
                    throw new ArgumentException("When in 'list' mode, path to accession directory file of format .csv expected (" + cliOptions.ToProcessDir.FullName + ")");
            }

            if (cliOptions.DirectoryFormat.ToLower().Equals("pacs"))
            {
                Logger.Info("Creating PACS directory finder");

                _ddf = new PacsDirectoryFinder(globals.FileSystemOptions.FileSystemRoot,
                    globals.FileSystemOptions.DicomSearchPattern, RabbitMqAdapter.SetupProducer(globals.ProcessDirectoryOptions.AccessionDirectoryProducerOptions, isBatch: false));
            }
            else if (cliOptions.DirectoryFormat.ToLower().Equals("list"))
            {
                Logger.Info("Creating accession directory lister");

                _ddf = new AccessionDirectoryLister(globals.FileSystemOptions.FileSystemRoot,
                    globals.FileSystemOptions.DicomSearchPattern, RabbitMqAdapter.SetupProducer(globals.ProcessDirectoryOptions.AccessionDirectoryProducerOptions, isBatch: false));
            }
            else if (cliOptions.DirectoryFormat.ToLower().Equals("default"))
            {
                Logger.Info("Creating basic directory finder");

                _ddf = new BasicDicomDirectoryFinder(globals.FileSystemOptions.FileSystemRoot,
                    globals.FileSystemOptions.DicomSearchPattern, RabbitMqAdapter.SetupProducer(globals.ProcessDirectoryOptions.AccessionDirectoryProducerOptions, isBatch: false));
            }
            else if (cliOptions.DirectoryFormat.ToLower().Equals("zips"))
            {
                Logger.Info("Creating zip directory finder");

                _ddf = new ZipDicomDirectoryFinder(globals.FileSystemOptions.FileSystemRoot,
                    globals.FileSystemOptions.DicomSearchPattern, RabbitMqAdapter.SetupProducer(globals.ProcessDirectoryOptions.AccessionDirectoryProducerOptions, isBatch: false));
            }
            else
            {
                throw new ArgumentException("Could not match directory format " + cliOptions.DirectoryFormat + " to an directory scan implementation");
            }
        }

        /// <summary>
        /// Searches from the given directory to look for DICOM files and writes AccessionDirectoryMessages to the message exchange
        /// </summary>
        public override void Start()
        {
            try
            {
                _ddf.SearchForDicomDirectories(_cliOptions.ToProcessDir.FullName);
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
