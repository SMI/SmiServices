
using Dicom;
using Microservices.Common.Messages;
using RabbitMQ.Client.Events;

namespace Microservices.DicomRelationalMapper.Messaging
{
    /// <summary>
    /// 
    /// </summary>
    public class QueuedImage
    {
        public IMessageHeader Header { get; set; }

        public BasicDeliverEventArgs BasicDeliverEventArgs { get; set; }

        public DicomFileMessage DicomFileMessage { get; set; }

        public DicomDataset DicomDataset { get; set; }


        public QueuedImage(IMessageHeader header, BasicDeliverEventArgs basicDeliverEventArgs, DicomFileMessage dicomFileMessage, DicomDataset dataset)
        {
            Header = header;
            BasicDeliverEventArgs = basicDeliverEventArgs;
            DicomFileMessage = dicomFileMessage;
            DicomDataset = dataset;
        }
    }
}