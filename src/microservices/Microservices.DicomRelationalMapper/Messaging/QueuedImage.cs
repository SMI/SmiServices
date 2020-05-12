
using Dicom;
using Smi.Common.Messages;
using RabbitMQ.Client.Events;

namespace Microservices.DicomRelationalMapper.Messaging
{
    /// <summary>
    /// 
    /// </summary>
    public class QueuedImage
    {
        public IMessageHeader Header { get; set; }

        public ulong tag { get; set; }

        public DicomFileMessage DicomFileMessage { get; set; }

        public DicomDataset DicomDataset { get; set; }


        public QueuedImage(IMessageHeader header, ulong _tag, DicomFileMessage dicomFileMessage, DicomDataset dataset)
        {
            Header = header;
            tag = _tag;
            DicomFileMessage = dicomFileMessage;
            DicomDataset = dataset;
        }
    }
}