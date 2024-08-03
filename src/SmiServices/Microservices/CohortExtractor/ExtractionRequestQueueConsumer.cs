using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using SmiServices.Microservices.CohortExtractor.Audit;
using SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System;
using System.ComponentModel;

namespace SmiServices.Microservices.CohortExtractor
{
    public class ExtractionRequestQueueConsumer : Consumer<ExtractionRequestMessage>
    {
        private readonly CohortExtractorOptions _options;

        private readonly IExtractionRequestFulfiller _fulfiller;
        private readonly IAuditExtractions _auditor;
        private readonly IProducerModel _fileMessageProducer;
        private readonly IProducerModel _fileMessageInfoProducer;

        private readonly IProjectPathResolver _resolver;

        public ExtractionRequestQueueConsumer(
            CohortExtractorOptions options,
            IExtractionRequestFulfiller fulfiller, IAuditExtractions auditor,
            IProjectPathResolver pathResolver, IProducerModel fileMessageProducer,
            IProducerModel fileMessageInfoProducer)
        {
            _options = options;
            _fulfiller = fulfiller;
            _auditor = auditor;
            _resolver = pathResolver;
            _fileMessageProducer = fileMessageProducer;
            _fileMessageInfoProducer = fileMessageInfoProducer;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, ExtractionRequestMessage request, ulong tag)
        {
            Logger.Info($"Received message {header.MessageGuid}: {request}");

            _auditor.AuditExtractionRequest(request);

            if (!request.ExtractionDirectory.StartsWith(request.ProjectNumber))
            {
                Logger.Debug("ExtractionDirectory did not start with the project number, doing ErrorAndNack");
                ErrorAndNack(header, tag, "", new InvalidEnumArgumentException("ExtractionDirectory"));
            }

            string extractionDirectory = request.ExtractionDirectory.TrimEnd('/', '\\');
            string? extractFileRoutingKey = request.IsIdentifiableExtraction ? _options.ExtractIdentRoutingKey : _options.ExtractAnonRoutingKey;

            foreach (ExtractImageCollection matchedFiles in _fulfiller.GetAllMatchingFiles(request, _auditor))
            {
                Logger.Info($"Accepted {matchedFiles.Accepted.Count} and rejected {matchedFiles.Rejected.Count} files for KeyValue {matchedFiles.KeyValue}");

                var infoMessage = new ExtractFileCollectionInfoMessage(request);

                foreach (QueryToExecuteResult accepted in matchedFiles.Accepted)
                {
                    var extractFileMessage = new ExtractFileMessage(request)
                    {
                        // Path to the original file
                        DicomFilePath = accepted.FilePathValue.TrimStart('/', '\\'),
                        // Extraction directory relative to the extract root
                        ExtractionDirectory = extractionDirectory,
                        // Output path for the anonymised file, relative to the extraction directory
                        OutputPath = _resolver.GetOutputPath(accepted, request).Replace('\\', '/')
                    };

                    Logger.Debug($"DicomFilePath={extractFileMessage.DicomFilePath}, OutputPath={extractFileMessage.OutputPath}");

                    // Send the extract file message
                    var sentHeader = (MessageHeader)_fileMessageProducer.SendMessage(extractFileMessage, header, extractFileRoutingKey);

                    // Record that we sent it
                    infoMessage.ExtractFileMessagesDispatched.Add(sentHeader, extractFileMessage.OutputPath);
                }

                // Wait for confirms from the batched messages
                Logger.Debug($"All ExtractFileMessage(s) sent for {matchedFiles.KeyValue}, calling WaitForConfirms");
                _fileMessageProducer.WaitForConfirms();

                // For all the rejected messages log why (in the info message)
                foreach (QueryToExecuteResult rejectedResults in matchedFiles.Rejected)
                {
                    var rejectReason = rejectedResults.RejectReason
                        ?? throw new ArgumentNullException(nameof(rejectedResults.RejectReason));

                    if (!infoMessage.RejectionReasons.ContainsKey(rejectReason))
                        infoMessage.RejectionReasons.Add(rejectReason, 0);

                    infoMessage.RejectionReasons[rejectReason]++;
                }

                _auditor.AuditExtractFiles(request, matchedFiles);

                infoMessage.KeyValue = matchedFiles.KeyValue;
                _fileMessageInfoProducer.SendMessage(infoMessage, header, routingKey: null);

                if (_fileMessageInfoProducer.GetType() == typeof(BatchProducerModel))
                    _fileMessageInfoProducer.WaitForConfirms();

                Logger.Info($"All ExtractFileCollectionInfoMessage(s) sent for {matchedFiles.KeyValue}");
            }

            Logger.Info($"Finished processing message {header.MessageGuid}");

            Ack(header, tag);
        }
    }
}
