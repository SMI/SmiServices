
using Microservices.CohortExtractor.Audit;
using Microservices.CohortExtractor.Execution;
using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using RabbitMQ.Client.Events;
using System;
using System.ComponentModel;
using System.Linq;

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

            Logger.Info("Received message for job " + request.ExtractionJobIdentifier);

            _auditor.AuditExtractionRequest(request);

            if (!CheckValidRequest(request, header, deliverArgs))
                return;

            foreach (ExtractImageCollection answers in _fulfiller.GetAllMatchingFiles(request, _auditor))
            {
                var infoMessage = new ExtractFileCollectionInfoMessage(request);

                foreach (QueryToExecuteResult accepted in answers.Accepted)
                {
                    var extractFileMessage = new ExtractFileMessage(request)
                    {
                        // Path to the original file
                        DicomFilePath = accepted.FilePathValue.TrimStart('/', '\\'),
                        // Extraction directory relative to the extract root
                        ExtractionDirectory = request.ExtractionDirectory.TrimEnd('/', '\\'),
                        // Output path for the anonymised file, relative to the extraction directory
                        OutputPath = _resolver.GetOutputPath(accepted,request).Replace('\\', '/')
                    };

                    Logger.Debug("DicomFilePath: " + extractFileMessage.DicomFilePath);
                    Logger.Debug("ExtractionDirectory: " + extractFileMessage.ExtractionDirectory);
                    Logger.Debug("OutputPath: " + extractFileMessage.OutputPath);

                    // Send the extract file message
                    var sentHeader = (MessageHeader)_fileMessageProducer.SendMessage(extractFileMessage, header);

                    // Record that we sent it
                    infoMessage.ExtractFileMessagesDispatched.Add(sentHeader, extractFileMessage.OutputPath);
                }

                //for all the rejected messages log why (in the info message)
                foreach (var rejectedResults in answers.Rejected)
                {
                    if(!infoMessage.RejectionReasons.ContainsKey(rejectedResults.RejectReason))
                        infoMessage.RejectionReasons.Add(rejectedResults.RejectReason,0);

                    infoMessage.RejectionReasons[rejectedResults.RejectReason]++;
                }

                _auditor.AuditExtractFiles(request, answers);

                infoMessage.KeyValue = answers.KeyValue;
                _fileMessageInfoProducer.SendMessage(infoMessage, header);
            }

            Ack(header, deliverArgs);
        }


        private bool CheckValidRequest(ExtractionRequestMessage request, IMessageHeader header, BasicDeliverEventArgs deliverArgs)
        {
            if (!request.ExtractionDirectory.StartsWith(request.ProjectNumber))
            {
                Logger.Debug("ExtractionDirectory did not start with the project number, doing ErrorAndNack for message (DeliveryTag " + deliverArgs.DeliveryTag + ")");
                ErrorAndNack(header, deliverArgs, "", new InvalidEnumArgumentException("ExtractionDirectory"));
                return false;
            }

            return true;
        }
    }
}