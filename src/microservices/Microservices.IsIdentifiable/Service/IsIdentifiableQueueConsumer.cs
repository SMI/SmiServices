using System;
using System.IO;
using System.Linq;
using Microservices.IsIdentifiable.Reporting;
using NLog;
using RabbitMQ.Client.Events;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Messaging;

namespace Microservices.IsIdentifiable.Service
{
    public class IsIdentifiableQueueConsumer : Consumer, IDisposable
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
            if (!SafeDeserializeToMessage(header, basicDeliverEventArgs, out ExtractFileStatusMessage message))
                return;

            var toProcess = new FileInfo( message.AnonymisedFileName);

            if(!toProcess.Exists)
                throw new FileNotFoundException();

            var result = _classifier.Classify(toProcess);

            bool isClean = true;
            foreach (Failure f in result)
            {
                Logger.Log(LogLevel.Info,$"Validation failed for {f.Resource} Problem Value:{f.ProblemValue}");
                isClean = false;
            }
            
            _producer.SendMessage(new IsIdentifiableMessage(message)
            {
                IsIdentifiable = isClean 
            }, header);

            Ack(header, basicDeliverEventArgs);
        }

        public void Dispose()
        {
            if(_classifier is IDisposable d)
                d.Dispose();
        }
    }
}