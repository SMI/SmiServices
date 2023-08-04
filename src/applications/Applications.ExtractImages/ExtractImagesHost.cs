using Smi.Common;
using Smi.Common.Execution;
using Smi.Common.Helpers;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;


namespace Applications.ExtractImages
{
    public class ExtractImagesHost : MicroserviceHost
    {
        private readonly IFileSystem _fileSystem;

        private readonly string _csvFilePath;

        private readonly IExtractionMessageSender _extractionMessageSender;

        private readonly string _absoluteExtractionDir;


        public ExtractImagesHost(
            GlobalOptions globals,
            ExtractImagesCliOptions cliOptions,
            IExtractionMessageSender? extractionMessageSender = null,
            IRabbitMqAdapter? rabbitMqAdapter = null,
            IFileSystem? fileSystem = null,
            bool threaded = false
        )
        : base(
            globals,
            rabbitMqAdapter,
            threaded
        )
        {
            ExtractImagesOptions options = Globals.ExtractImagesOptions;
            if (options == null)
                throw new ArgumentException(nameof(Globals.ExtractImagesOptions));

            _fileSystem = fileSystem ?? new FileSystem();

            string extractRoot = Globals.FileSystemOptions.ExtractRoot;
            if (!_fileSystem.Directory.Exists(extractRoot))
                throw new DirectoryNotFoundException($"Could not find the extraction root '{extractRoot}'");

            _csvFilePath = cliOptions.CohortCsvFile;
            if (string.IsNullOrWhiteSpace(_csvFilePath))
                throw new ArgumentNullException(nameof(cliOptions.CohortCsvFile));
            if (!_fileSystem.File.Exists(_csvFilePath))
                throw new FileNotFoundException($"Could not find the cohort CSV file '{_csvFilePath}'");

            // TODO(rkm 2021-04-01) Now that all the extraction path code is in C#, we would benefit from refactoring it all out
            //                      to a helper class to support having multiple configurations (and probably prevent some bugs)
            string extractionName = _fileSystem.Path.GetFileNameWithoutExtension(_csvFilePath);
            string extractionDir = _fileSystem.Path.Join(cliOptions.ProjectId, "extractions", extractionName);
            _absoluteExtractionDir = _fileSystem.Path.Join(extractRoot, extractionDir);

            if (_fileSystem.Directory.Exists(_absoluteExtractionDir))
                throw new DirectoryNotFoundException($"Extraction directory already exists '{_absoluteExtractionDir}'");

            if (extractionMessageSender == null)
            {
                IProducerModel extractionRequestProducer = RabbitMqAdapter.SetupProducer(options.ExtractionRequestProducerOptions, isBatch: false);
                IProducerModel extractionRequestInfoProducer = RabbitMqAdapter.SetupProducer(options.ExtractionRequestInfoProducerOptions, isBatch: false);

                _extractionMessageSender = new ExtractionMessageSender(
                    options,
                    cliOptions,
                    extractionRequestProducer,
                    extractionRequestInfoProducer,
                    _fileSystem,
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
            var parser = new CohortCsvParser(_fileSystem);
            (ExtractionKey extractionKey, List<string> idList) = parser.Parse(_csvFilePath);

            _extractionMessageSender.SendMessages(extractionKey, idList);

            Stop("Completed");
        }
    }
}
