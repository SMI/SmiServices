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

        protected override void ProcessMessageImpl(IMessageHeader header, ExtractedFileStatusMessage message, ulong tag)
        {
            // We should only ever receive messages regarding anonymised images
            if (message.Status != ExtractedFileStatus.Anonymised)
                throw new ApplicationException($"Received an {message.GetType().Name} message with status '{message.Status}' and message '{message.StatusMessage}'");

            IFileInfo toProcess = _fileSystem.FileInfo.FromFileName(
                _fileSystem.Path.Combine(
                    _extractionRoot,
                    message.ExtractionDirectory,
                    message.OutputFilePath
                )
            );

            if (!toProcess.Exists)
            {
                ErrorAndNack(
                    header,
                    tag,
                    $"Exception while processing {message.GetType().Name}",
                    new ApplicationException($"Could not find '{toProcess.FullName}'")
                );
                return;
            }

            IEnumerable<Failure> failures;

            try
            {
                failures = _classifier.Classify(toProcess);
            }
            catch (ArithmeticException ae)
            {
                ErrorAndNack(header, tag, $"Exception while classifying {message.GetType().Name}", ae);
                return;
            }

            foreach (Failure f in failures)
                Logger.Info($"Validation failed for {f.Resource} Problem Value:{f.ProblemValue}");

            var response = new ExtractedFileVerificationMessage(message)
            {
                IsIdentifiable = failures.Any(),
                Report = JsonConvert.SerializeObject(failures),
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