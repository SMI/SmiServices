using System;
using System.IO;
using System.IO.Abstractions;
using NLog;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using Newtonsoft.Json;
using IsIdentifiable.Reporting;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableQueueConsumer : Consumer<ExtractedFileStatusMessage>, IDisposable
    {
        private readonly IProducerModel _producer;
        private readonly IFileSystem _fileSystem;
        private readonly string _extractionRoot;
        private readonly IClassifier _classifier;

        public IsIdentifiableQueueConsumer(
            IProducerModel producer,
            string extractionRoot,
            IClassifier classifier,
            IFileSystem fileSystem = null
        )
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _extractionRoot = string.IsNullOrWhiteSpace(extractionRoot) ? throw new ArgumentException($"Argument cannot be null or whitespace", nameof(extractionRoot)) : extractionRoot; ;
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _fileSystem = fileSystem ?? new FileSystem();

            if (!_fileSystem.Directory.Exists(_extractionRoot))
                throw new DirectoryNotFoundException($"Could not find the extraction root '{_extractionRoot}' in the filesystem");
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