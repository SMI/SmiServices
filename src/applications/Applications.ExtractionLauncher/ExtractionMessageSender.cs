using NLog;
using Smi.Common.Helpers;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Applications.ExtractionLauncher
{
    public class ExtractionMessageSender : IExtractionMessageSender
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IProducerModel _extractionRequestProducer;
        private readonly IProducerModel _extractionRequestInfoProducer;

        private readonly string _extractionDir;

        private readonly DateTimeProvider _dateTimeProvider;
        private readonly IConsoleInput _consoleInput;

        private readonly int _maxIdentifiersPerMessage;

        private readonly string _projectId;
        private readonly string[] _modalities;
        private readonly bool _isIdentifiableExtraction;
        private readonly bool _isNoFiltersExtraction;
        private readonly bool _nonInteractive;


        public ExtractionMessageSender(
            ExtractionLauncherOptions options,
            ExtractionLauncherCliOptions cliOptions,
            IProducerModel extractionRequestProducer,
            IProducerModel extractionRequestInfoProducer,
            string extractionDir,
            DateTimeProvider dateTimeProvider,
            IConsoleInput consoleInput
        )
        {
            _extractionRequestProducer = extractionRequestProducer;
            _extractionRequestInfoProducer = extractionRequestInfoProducer;

            _extractionDir = (!string.IsNullOrWhiteSpace(extractionDir)) ? extractionDir : throw new ArgumentException(nameof(extractionDir));
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
            string modalitiesString = _modalities == null ? null : string.Join(',', _modalities);

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
                ExtractionIdentifiers = null,
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
                ExtractionModality = modalitiesString,
            };

            if (_nonInteractive)
            {
                SendMessagesImpl(ermList, erim);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"ExtractionJobIdentifier:        {jobId}");
                sb.AppendLine($"ProjectNumber:                  {_projectId}");
                sb.AppendLine($"ExtractionDirectory:            {_extractionDir}");
                sb.AppendLine($"ExtractionKey:                  {extractionKey}");
                sb.AppendLine($"IsIdentifiableExtraction:       {_isIdentifiableExtraction}");
                sb.AppendLine($"IsNoFilterExtraction:           {_isNoFiltersExtraction}");
                sb.AppendLine($"ExtractionModality:             {modalitiesString ?? "<unspecified>"}");
                sb.AppendLine($"KeyValueCount:                  {idList.Count}");
                sb.AppendLine($"ExtractionRequestMessage count: {ermList.Count}");
                _logger.Info(sb.ToString());
                LogManager.Flush();
                Console.WriteLine("Confirm you want to start an extract job with the above information");

                string key;
                do
                {
                    Console.Write("[y/n]: ");
                    key = _consoleInput.GetNextLine()?.ToLower();
                } while (key != "y" && key != "n");

                if (key == "y")
                {
                    SendMessagesImpl(ermList, erim);
                }
                else
                {
                    _logger.Info("Operation cancelled by user");
                }
            }
        }

        private void SendMessagesImpl(IEnumerable<ExtractionRequestMessage> ermList, ExtractionRequestInfoMessage erim)
        {
            _logger.Info("Sending messages");

            foreach (var msg in ermList)
                _extractionRequestProducer.SendMessage(msg, isInResponseTo: null);

            _extractionRequestInfoProducer.SendMessage(erim, isInResponseTo: null);

            _logger.Info("All messages sent");
        }
    }
}