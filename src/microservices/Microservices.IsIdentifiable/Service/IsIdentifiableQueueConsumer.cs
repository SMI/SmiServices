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
        private readonly string _fileSystemRoot;

        public IsIdentifiableQueueConsumer(string fileSystemRoot)
        {
            _fileSystemRoot = fileSystemRoot;
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
            
            Ack(header, basicDeliverEventArgs);

            throw new NotImplementedException();
        }
    }
}