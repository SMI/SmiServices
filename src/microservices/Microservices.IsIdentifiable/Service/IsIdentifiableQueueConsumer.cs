using IsIdentifiable.Reporting;
using Newtonsoft.Json;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

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
            _extractionRoot = string.IsNullOrWhiteSpace(extractionRoot) ? throw new ArgumentException($"Argument cannot be null or whitespace", nameof(extractionRoot)) : extractionRoot;
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _fileSystem = fileSystem ?? new FileSystem();

            if (!_fileSystem.Directory.Exists(_extractionRoot))
                throw new DirectoryNotFoundException($"Could not find the extraction root '{_extractionRoot}' in the filesystem");
        }

        protected override void ProcessMessageImpl(IMessageHeader? header, ExtractedFileStatusMessage statusMessage, ulong tag)
        {
            // We should only ever receive messages regarding anonymised images
            if (statusMessage.Status != ExtractedFileStatus.Anonymised)
                throw new ApplicationException($"Received an {statusMessage.GetType().Name} message with Status '{statusMessage.Status}' and StatusMessage '{statusMessage.StatusMessage}'");

            IFileInfo toProcess = _fileSystem.FileInfo.New(
                _fileSystem.Path.Combine(
                    _extractionRoot,
                    statusMessage.ExtractionDirectory,
                    statusMessage.OutputFilePath
                )
            );

            if (!toProcess.Exists)
            {
                SendVerificationMessage(statusMessage, header, tag, VerifiedFileStatus.ErrorWontRetry, $"Exception while processing {statusMessage.GetType().Name}: Could not find file to process '{toProcess.FullName}'");
                return;
            }

            IEnumerable<Failure> failures;

            try
            {
                failures = _classifier.Classify(toProcess);
            }
            catch (ArithmeticException ae)
            {
                SendVerificationMessage(statusMessage, header, tag, VerifiedFileStatus.ErrorWontRetry, $"Exception while classifying {statusMessage.GetType().Name}:\n{ae}");
                return;
            }

            foreach (Failure f in failures)
                Logger.Info($"Validation failed for {f.Resource} Problem Value:{f.ProblemValue}");


            var status = failures.Any() ? VerifiedFileStatus.IsIdentifiable : VerifiedFileStatus.NotIdentifiable;
            var report = JsonConvert.SerializeObject(failures);

            SendVerificationMessage(statusMessage, header, tag, status, report);
        }

        private void SendVerificationMessage(
            ExtractedFileStatusMessage statusMessage,
            IMessageHeader header,
            ulong tag,
            VerifiedFileStatus status,
            string report
        )
        {
            var response = new ExtractedFileVerificationMessage(statusMessage)
            {
                Status = status,
                Report = report,
            };
            _producer.SendMessage(response, header);

            Ack(header, tag);
        }

        public void Dispose()
        {
            if (_classifier is IDisposable d)
                d.Dispose();
        }
    }
}
