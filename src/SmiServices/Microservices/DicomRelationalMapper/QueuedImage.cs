
using FellowOakDicom;
using SmiServices.Common.Messages;

namespace SmiServices.Microservices.DicomRelationalMapper
{
    /// <summary>
    /// 
    /// </summary>
    public class QueuedImage
    {
        public IMessageHeader Header { get; init; }

        public ulong Tag { get; init; }

        public DicomFileMessage DicomFileMessage { get; init; }

        public DicomDataset DicomDataset { get; init; }

        public QueuedImage(IMessageHeader header, ulong tag, DicomFileMessage dicomFileMessage, DicomDataset dataset)
        {
            Header = header;
            Tag = tag;
            DicomFileMessage = dicomFileMessage;
            DicomDataset = dataset;
        }
    }
}
