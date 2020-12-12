using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microservices.IsIdentifiable.Reporting;
using NLog;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Newtonsoft.Json;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableQueueConsumer : Consumer<ExtractedFileStatusMessage>, IDisposable
    {
        private readonly IProducerModel _producer;
        private readonly IFileSystem _fileSystem = new FileSystem();
        private readonly string _fileSystemRoot;
        private readonly string _extractionRoot;
        private readonly IClassifier _classifier;

        public IsIdentifiableQueueConsumer(IProducerModel producer, string fileSystemRoot, string extractionRoot, IClassifier classifier)
        {
            _producer = producer;
            _fileSystemRoot = fileSystemRoot;
            _extractionRoot = extractionRoot;
            _classifier = classifier;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, ExtractedFileStatusMessage message, ulong tag)
        {
            bool isClean = true;
            object resultObject;

            try
            {
                // We should only ever receive messages regarding anonymised images
                if (message.Status != ExtractedFileStatus.Anonymised)
                    throw new ApplicationException($"Received a message with anonymised status of {message.Status}");

                IFileInfo toProcess = _fileSystem.FileInfo.FromFileName( Path.Combine(_extractionRoot, message.ExtractionDirectory, message.OutputFilePath) );

                if(!toProcess.Exists)
                    throw new ApplicationException("IsIdentifiable service cannot find file "+toProcess.FullName);

                var result = _classifier.Classify(toProcess);

                foreach (Failure f in result)
                {
                    Logger.Log(LogLevel.Info,$"Validation failed for {f.Resource} Problem Value:{f.ProblemValue}");
                    isClean = false;
                }
                resultObject = result;
            }
            catch (ApplicationException e)
            {
                // Catch specific exceptions we are aware of, any uncaught will bubble up to the wrapper
                ErrorAndNack(header, tag, "Error while processing AnonSuccessMessage", e);
                return;
            }

            _producer.SendMessage(new ExtractedFileVerificationMessage(message)
            {
                IsIdentifiable = ! isClean,
                Report = JsonConvert.SerializeObject(resultObject)
            }, header);

            Ack(header, tag);
        }

        public void Dispose()
        {
            if(_classifier is IDisposable d)
                d.Dispose();
        }
    }
}