using System;
using System.IO;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableQueueConsumer : Consumer
    {
        private readonly IProducerModel _producer;
        private readonly string _fileSystemRoot;
        private readonly IClassifier _classifier;

        public IsIdentifiableQueueConsumer(IProducerModel producer, string fileSystemRoot, IClassifier classifier)
        {
            _producer = producer;
            _fileSystemRoot = fileSystemRoot;
            _classifier = classifier;
        }

        protected override void ProcessMessageImpl(IMessageHeader header, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            // Deserialize the message from the delivery arguments
            ExtractFileMessage message;
            if (!SafeDeserializeToMessage(header, basicDeliverEventArgs, out message))
                return;

            var toProcess = new FileInfo(Path.Combine(_fileSystemRoot, message.DicomFilePath));

            if(!toProcess.Exists)
                throw new FileNotFoundException();

            var result = _classifier.Classify(toProcess);
            
            _producer.SendMessage(new IsIdentifiableMessage {IsIdentifiable = result != null}, header);

            if(result == null)
                Ack(header, basicDeliverEventArgs);
        }
    }
}