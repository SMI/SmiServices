using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Microservices.IdentifierMapper.Execution.Swappers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Smi.Common.Options;
using System;
using System.ComponentModel;

namespace Microservices.CohortExtractor.Messaging
{
    public class ExtractionRequestQueueConsumer : Consumer<ExtractionRequestMessage>
    {
        private readonly CohortExtractorOptions _options;

        private readonly IExtractionRequestFulfiller _fulfiller;
        private readonly IAuditExtractions _auditor;
        private readonly IProducerModel _fileMessageProducer;
        private readonly IProducerModel _fileMessageInfoProducer;
        private readonly ISwapIdentifiers _uidSwapper;

        private readonly IProjectPathResolver _resolver;

        public ExtractionRequestQueueConsumer(
            CohortExtractorOptions options,
            IExtractionRequestFulfiller fulfiller, IAuditExtractions auditor,
            IProjectPathResolver pathResolver, IProducerModel fileMessageProducer,
            IProducerModel fileMessageInfoProducer,
            ISwapIdentifiers uidSwapper = null
        )
        {
            _options = options;
            _fulfiller = fulfiller;
            _auditor = auditor;
            _resolver = pathResolver;
            _fileMessageProducer = fileMessageProducer;
            _fileMessageInfoProducer = fileMessageInfoProducer;
            _uidSwapper = uidSwapper;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, ExtractionRequestMessage request, ulong tag)
        {
            Logger.Info($"Received message: {request}");

            _auditor.AuditExtractionRequest(request);

            if (!request.ExtractionDirectory.StartsWith(request.ProjectNumber))
            {
                Logger.Debug("ExtractionDirectory did not start with the project number, doing ErrorAndNack");
                ErrorAndNack(header, tag, "", new InvalidEnumArgumentException("ExtractionDirectory"));
            }

            string extractionDirectory = request.ExtractionDirectory.TrimEnd('/', '\\');
            string extractFileRoutingKey = request.IsIdentifiableExtraction ? _options.ExtractIdentRoutingKey : _options.ExtractAnonRoutingKey;

            foreach (ExtractImageCollection matchedFiles in _fulfiller.GetAllMatchingFiles(request, _auditor))
            {
                Logger.Info($"Accepted {matchedFiles.Accepted.Count} and rejected {matchedFiles.Rejected.Count} files for KeyValue {matchedFiles.KeyValue}");

                var infoMessage = new ExtractFileCollectionInfoMessage(request);

                foreach (QueryToExecuteResult accepted in matchedFiles.Accepted)
                {
                    var extractFileMessage = new ExtractFileMessage()
                    {
                        // Path to the original file
                        DicomFilePath = accepted.FilePathValue.TrimStart('/', '\\'),
                        // Extraction directory relative to the extract root
                        ExtractionDirectory = extractionDirectory,
                        // Output path for the anonymised file, relative to the extraction directory
                        OutputPath = _resolver.GetOutputPath(accepted, request).Replace('\\', '/'),

                        ReplacementStudyInstanceUID = request.IsIdentifiableExtraction ? null : SwapIfApplicable("StudyInstanceUID", accepted.StudyTagValue),
                        ReplacementSeriesInstanceUID = request.IsIdentifiableExtraction ? null : SwapIfApplicable("SeriesInstanceUID", accepted.SeriesTagValue),
                        ReplacementSOPInstanceUID =  request.IsIdentifiableExtraction ? null : SwapIfApplicable("SOPInstanceUID", accepted.InstanceTagValue),
                    };

                    Logger.Debug($"DicomFilePath={extractFileMessage.DicomFilePath}, OutputPath={extractFileMessage.OutputPath}");

                    // Send the extract file message
                    var sentHeader = (MessageHeader)_fileMessageProducer.SendMessage(extractFileMessage, header, extractFileRoutingKey);

                    // Record that we sent it
                    infoMessage.ExtractFileMessagesDispatched.Add(sentHeader, extractFileMessage.OutputPath);
                }

                // Wait for confirms from the batched messages
                Logger.Debug($"All file messages sent for {request.ExtractionJobIdentifier}, calling WaitForConfirms");
                _fileMessageProducer.WaitForConfirms();

                // For all the rejected messages log why (in the info message)
                foreach (QueryToExecuteResult rejectedResults in matchedFiles.Rejected)
                {
                    if (!infoMessage.RejectionReasons.ContainsKey(rejectedResults.RejectReason))
                        infoMessage.RejectionReasons.Add(rejectedResults.RejectReason, 0);

                    infoMessage.RejectionReasons[rejectedResults.RejectReason]++;
                }

                _auditor.AuditExtractFiles(request, matchedFiles);

                infoMessage.KeyValue = matchedFiles.KeyValue;
                _fileMessageInfoProducer.SendMessage(infoMessage, header);

                if (_fileMessageInfoProducer.GetType() == typeof(BatchProducerModel))
                    _fileMessageInfoProducer.WaitForConfirms();

                Logger.Info($"All messages sent and acknowledged for {matchedFiles.KeyValue}");
            }

            Logger.Info("Finished processing message");
            Ack(header, tag);
        }

        private string SwapIfApplicable(string tagName, string value)
        {
            if (_uidSwapper == null)
                return null;

            var replacement = _uidSwapper.GetSubstitutionFor(value, out string reason);

            if (string.IsNullOrWhiteSpace(replacement) || reason != null)
                throw new Exception($"Couldn't get a replacement {tagName} for {value}. Reason: '{reason}'");
            
            return replacement;
        }
    }
}
