
using System.ComponentModel;
using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;

namespace Microservices.CohortExtractor.Messaging
{
    public class ExtractionRequestQueueConsumer : Consumer
    {
        private readonly IExtractionRequestFulfiller _fulfiller;
        private readonly IAuditExtractions _auditor;
        private readonly IProducerModel _fileMessageProducer;
        private readonly IProducerModel _fileMessageInfoProducer;

        private readonly IProjectPathResolver _resolver;

        public ExtractionRequestQueueConsumer(IExtractionRequestFulfiller fulfiller, IAuditExtractions auditor,
            IProjectPathResolver pathResolver, IProducerModel fileMessageProducer,
            IProducerModel fileMessageInfoProducer)
        {
            _fulfiller = fulfiller;
            _auditor = auditor;
            _resolver = pathResolver;
            _fileMessageProducer = fileMessageProducer;
            _fileMessageInfoProducer = fileMessageInfoProducer;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs deliverArgs)
        {
            ExtractionRequestMessage request;
            if (!SafeDeserializeToMessage(header, deliverArgs, out request))
                return;

            Logger.Info($"Received message: {request}");

            _auditor.AuditExtractionRequest(request);

            if (!request.ExtractionDirectory.StartsWith(request.ProjectNumber))
            {
                Logger.Debug("ExtractionDirectory did not start with the project number, doing ErrorAndNack");
                ErrorAndNack(header, deliverArgs, "", new InvalidEnumArgumentException("ExtractionDirectory"));
            }

            string extractionDirectory = request.ExtractionDirectory.TrimEnd('/', '\\');

            foreach (ExtractImageCollection matchedFiles in _fulfiller.GetAllMatchingFiles(request, _auditor))
            {
                Logger.Info($"Matched {matchedFiles.Accepted.Count} files with {matchedFiles.Rejected.Count} for KeyValue {matchedFiles.KeyValue}");

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
                    var sentHeader = (MessageHeader)_fileMessageProducer.SendMessage(extractFileMessage, header);

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
            Ack(header, deliverArgs);
        }
    }
}