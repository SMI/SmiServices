using SmiServices.Common;
using SmiServices.Common.Execution;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;


namespace SmiServices.Applications.ExtractImages
{
    public class ExtractImagesHost : MicroserviceHost
    {
        private readonly string _csvFilePath;

        private readonly IExtractionMessageSender _extractionMessageSender;

        private readonly string _absoluteExtractionDir;


        public ExtractImagesHost(
            GlobalOptions globals,
            ExtractImagesCliOptions cliOptions,
            IFileSystem? fileSystem = null,
            IExtractionMessageSender? extractionMessageSender = null,
            IMessageBroker? messageBroker = null,
            bool threaded = false
        )
        : base(
            globals,
            fileSystem ?? new FileSystem(),
            messageBroker,
            threaded
        )
        {
            ExtractImagesOptions? options = Globals.ExtractImagesOptions ?? throw new ArgumentException(nameof(Globals.ExtractImagesOptions));

            string extractRoot = Globals.FileSystemOptions?.ExtractRoot ?? throw new ArgumentException("Some part of Globals.FileSystemOptions.ExtractRoot was null");
            if (!FileSystem.Directory.Exists(extractRoot))
                throw new DirectoryNotFoundException($"Could not find the extraction root '{extractRoot}'");

            _csvFilePath = cliOptions.CohortCsvFile;
            if (string.IsNullOrWhiteSpace(_csvFilePath))
                throw new ArgumentNullException(nameof(cliOptions));
            if (!FileSystem.File.Exists(_csvFilePath))
                throw new FileNotFoundException($"Could not find the cohort CSV file '{_csvFilePath}'");

            // TODO(rkm 2021-04-01) Now that all the extraction path code is in C#, we would benefit from refactoring it all out
            //                      to a helper class to support having multiple configurations (and probably prevent some bugs)
            string extractionName = FileSystem.Path.GetFileNameWithoutExtension(_csvFilePath);
            string extractionDir = FileSystem.Path.Join(cliOptions.ProjectId, "extractions", extractionName);
            _absoluteExtractionDir = FileSystem.Path.Join(extractRoot, extractionDir);

            if (FileSystem.Directory.Exists(_absoluteExtractionDir))
                throw new DirectoryNotFoundException($"Extraction directory already exists '{_absoluteExtractionDir}'");

            if (extractionMessageSender == null)
            {
                IProducerModel extractionRequestProducer = MessageBroker.SetupProducer(options.ExtractionRequestProducerOptions!, isBatch: false);
                IProducerModel extractionRequestInfoProducer = MessageBroker.SetupProducer(options.ExtractionRequestInfoProducerOptions!, isBatch: false);

                _extractionMessageSender = new ExtractionMessageSender(
                    options,
                    cliOptions,
                    extractionRequestProducer,
                    extractionRequestInfoProducer,
                    FileSystem,
                    extractRoot,
                    extractionDir,
                    new DateTimeProvider(),
                    new RealConsoleInput()
                );
            }
            else
            {
                Logger.Warn($"{nameof(Globals.ExtractImagesOptions.MaxIdentifiersPerMessage)} will be ignored here");
                _extractionMessageSender = extractionMessageSender;
            }
        }

        public override void Start()
        {
            var parser = new CohortCsvParser(FileSystem);
            (ExtractionKey extractionKey, List<string> idList) = parser.Parse(_csvFilePath);

            _extractionMessageSender.SendMessages(extractionKey, idList);

            Stop("Completed");
        }
    }
}
