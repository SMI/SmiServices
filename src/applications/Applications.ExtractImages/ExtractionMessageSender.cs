using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using NLog;
using Smi.Common.Helpers;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;


namespace Applications.ExtractImages
{
    public class ExtractionMessageSender : IExtractionMessageSender
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IProducerModel _extractionRequestProducer;
        private readonly IProducerModel _extractionRequestInfoProducer;
        private readonly IFileSystem _fileSystem;
        private readonly string _extractionRoot;
        private readonly string _extractionDir;

        private readonly DateTimeProvider _dateTimeProvider;
        private readonly IConsoleInput _consoleInput;

        private readonly int _maxIdentifiersPerMessage;

        private readonly string _projectId;
        private readonly string[]? _modalities;
        private readonly bool _isIdentifiableExtraction;
        private readonly bool _isNoFiltersExtraction;
        private readonly bool _nonInteractive;


        public ExtractionMessageSender(
            ExtractImagesOptions options,
            ExtractImagesCliOptions cliOptions,
            IProducerModel extractionRequestProducer,
            IProducerModel extractionRequestInfoProducer,
            IFileSystem fileSystem,
            string? extractionRoot,
            string? extractionDir,
            DateTimeProvider dateTimeProvider,
            IConsoleInput consoleInput
        )
        {
            _extractionRequestProducer = extractionRequestProducer;
            _extractionRequestInfoProducer = extractionRequestInfoProducer;

            _fileSystem = fileSystem;
            _extractionRoot = (!string.IsNullOrWhiteSpace(extractionRoot)) ? extractionRoot : throw new ArgumentException(null, nameof(extractionRoot));
            _extractionDir = (!string.IsNullOrWhiteSpace(extractionDir)) ? extractionDir : throw new ArgumentException(null, nameof(extractionDir));
            _dateTimeProvider = dateTimeProvider;
            _consoleInput = consoleInput;

            _maxIdentifiersPerMessage = options.MaxIdentifiersPerMessage;
            if (_maxIdentifiersPerMessage <= 0)
                throw new ArgumentOutOfRangeException(nameof(options.MaxIdentifiersPerMessage));

            _projectId = (!string.IsNullOrWhiteSpace(cliOptions.ProjectId)) ? cliOptions.ProjectId : throw new ArgumentException(nameof(cliOptions.ProjectId));
            _modalities = cliOptions.Modalities?.ToUpper().Split(',', StringSplitOptions.RemoveEmptyEntries);
            _isIdentifiableExtraction = cliOptions.IsIdentifiableExtraction;
            _isNoFiltersExtraction = cliOptions.IsNoFiltersExtraction;
            _nonInteractive = cliOptions.NonInteractive;
        }

        public void SendMessages(ExtractionKey extractionKey, List<string> idList)
        {
            if (idList.Count == 0)
                throw new ArgumentException("ID list is empty");

            var jobId = Guid.NewGuid();
            DateTime now = _dateTimeProvider.UtcNow();

            // TODO(rkm 2021-04-01) Change this to a string[] in both messages below
            string? modalitiesString = _modalities == null ? null : string.Join(',', _modalities);
            string userName = Environment.UserName;

            var erm = new ExtractionRequestMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = _projectId,
                ExtractionDirectory = _extractionDir,
                JobSubmittedAt = now,
                IsIdentifiableExtraction = _isIdentifiableExtraction,
                IsNoFilterExtraction = _isNoFiltersExtraction,

                // TODO(rkm 2021-04-01) Change this to an ExtractionKey type
                KeyTag = extractionKey.ToString(),

                Modalities = modalitiesString,

                // NOTE(rkm 2021-04-01) Set below
                ExtractionIdentifiers = null!,
            };

            List<ExtractionRequestMessage> ermList =
                idList
                .Chunk(_maxIdentifiersPerMessage)
                .Select(x =>
                    new ExtractionRequestMessage(erm)
                    {
                        ExtractionIdentifiers = x.ToList()
                    }
            ).ToList();

            var erim = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = _projectId,
                ExtractionDirectory = _extractionDir,
                JobSubmittedAt = now,
                IsIdentifiableExtraction = _isIdentifiableExtraction,
                IsNoFilterExtraction = _isNoFiltersExtraction,

                KeyTag = extractionKey.ToString(),
                KeyValueCount = idList.Count,
                UserName = userName,
                ExtractionModality = modalitiesString,
            };

            if (_nonInteractive)
            {
                LaunchExtraction(jobId, ermList, erim);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"ExtractionJobIdentifier:        {jobId}");
                sb.AppendLine($"Submitted:                      {now:u}");
                sb.AppendLine($"ProjectNumber:                  {_projectId}");
                sb.AppendLine($"ExtractionDirectory:            {_extractionDir}");
                sb.AppendLine($"ExtractionKey:                  {extractionKey}");
                sb.AppendLine($"IsIdentifiableExtraction:       {_isIdentifiableExtraction}");
                sb.AppendLine($"IsNoFilterExtraction:           {_isNoFiltersExtraction}");
                sb.AppendLine($"ExtractionModality:             {modalitiesString ?? "<unspecified>"}");
                sb.AppendLine($"UserName:                       {userName}");
                sb.AppendLine($"KeyValueCount:                  {idList.Count}");
                sb.AppendLine($"ExtractionRequestMessage count: {ermList.Count}");
                _logger.Info(sb.ToString());
                LogManager.Flush();
                Console.WriteLine("Confirm you want to start an extract job with the above information");

                string? key;
                do
                {
                    Console.Write("[y/n]: ");
                    key = _consoleInput.GetNextLine()?.ToLower();
                } while (key != "y" && key != "n");

                if (key == "y")
                {
                    LaunchExtraction(jobId, ermList, erim);
                }
                else
                {
                    _logger.Info("Operation cancelled by user");
                }
            }
        }

        private void LaunchExtraction(Guid jobId, IEnumerable<ExtractionRequestMessage> ermList, ExtractionRequestInfoMessage erim)
        {
            InitialiseExtractionDir(jobId);
            SendMessagesImpl(ermList, erim);
        }

        private void InitialiseExtractionDir(Guid jobId)
        {
            var absoluteExtractionDir = _fileSystem.Path.Combine(_extractionRoot, _extractionDir);
            _fileSystem.Directory.CreateDirectory(absoluteExtractionDir);

            // Write the jobId to a file in the extraction dir to help identify the set of files if they are moved
            string jobIdFile = _fileSystem.Path.Combine(_extractionRoot, _extractionDir, "jobId.txt");
            _fileSystem.File.WriteAllText(jobIdFile, $"{jobId}\n");

            _logger.Info("Created extraction directory and jobId file");
        }

        private void SendMessagesImpl(IEnumerable<ExtractionRequestMessage> ermList, ExtractionRequestInfoMessage erim)
        {
            _logger.Info("Sending messages");

            foreach (var msg in ermList)
                _extractionRequestProducer.SendMessage(msg, isInResponseTo: null, routingKey: null);

            _extractionRequestInfoProducer.SendMessage(erim, isInResponseTo: null, routingKey: null);

            _logger.Info("All messages sent");
        }
    }
}
