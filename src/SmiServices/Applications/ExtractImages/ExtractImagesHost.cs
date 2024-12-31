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
using System.Linq;


namespace SmiServices.Applications.ExtractImages
{
    public class ExtractImagesHost : MicroserviceHost
    {
        private readonly IFileSystem _fileSystem;

        private readonly string _csvFilePath;

        private readonly IExtractionMessageSender _extractionMessageSender;

        private readonly string _absoluteExtractionDir;

        private readonly ExtractionKey[]? _allowedKeys;

        public ExtractImagesHost(
            GlobalOptions globals,
            ExtractImagesCliOptions cliOptions,
            IExtractionMessageSender? extractionMessageSender = null,
            IMessageBroker? messageBroker = null,
            IFileSystem? fileSystem = null,
            bool threaded = false
        )
        : base(
            globals,
            messageBroker)
        {
            ExtractImagesOptions? options = Globals.ExtractImagesOptions ?? throw new ArgumentException(nameof(Globals.ExtractImagesOptions));
            _allowedKeys = options.AllowedExtractionKeys;

            _fileSystem = fileSystem ?? new FileSystem();

            string extractRoot = Globals.FileSystemOptions?.ExtractRoot ?? throw new ArgumentException("Some part of Globals.FileSystemOptions.ExtractRoot was null");
            if (!_fileSystem.Directory.Exists(extractRoot))
                throw new DirectoryNotFoundException($"Could not find the extraction root '{extractRoot}'");

            if (cliOptions.IsPooledExtraction)
            {
                if (!_fileSystem.Directory.Exists(Globals.FileSystemOptions.ExtractionPoolRoot))
                    throw new InvalidOperationException($"{nameof(cliOptions.IsPooledExtraction)} can only be passed if {nameof(Globals.FileSystemOptions.ExtractionPoolRoot)} is a directory");

                if (cliOptions.IsIdentifiableExtraction)
                    throw new InvalidOperationException($"{nameof(cliOptions.IsPooledExtraction)} is incompatible with {nameof(cliOptions.IsIdentifiableExtraction)}");

                if (cliOptions.IsNoFiltersExtraction)
                    throw new InvalidOperationException($"{nameof(cliOptions.IsPooledExtraction)} is incompatible with {nameof(cliOptions.IsNoFiltersExtraction)}");
            }

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
                IProducerModel extractionRequestProducer = MessageBroker.SetupProducer(options.ExtractionRequestProducerOptions!, isBatch: false);
                IProducerModel extractionRequestInfoProducer = MessageBroker.SetupProducer(options.ExtractionRequestInfoProducerOptions!, isBatch: false);

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

            if (_allowedKeys?.Contains(extractionKey) == false)
                throw new InvalidOperationException($"'{extractionKey}' from CSV not in list of supported extraction keys ({string.Join(',', _allowedKeys)})");

            _extractionMessageSender.SendMessages(extractionKey, idList);

            Stop("Completed");
        }
    }
}
