using FellowOakDicom;
using Microservices.DicomRelationalMapper.Messaging;
using System.Collections.Generic;
using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;

namespace Microservices.DicomRelationalMapper.Execution;

public class DicomFileMessageToDatasetListWorklist : IDicomDatasetWorklist
{
    private readonly IEnumerator<QueuedImage> _messages;
    public int Count { get; private set; } = 0;

    public readonly HashSet<QueuedImage> CorruptMessages = new();

    public DicomFileMessageToDatasetListWorklist(IEnumerable<QueuedImage> messages)
    {
        _messages = messages.GetEnumerator();
    }

    /// <summary>
    /// Resets the progress through the work list e.g. if half the list is consumed and you want to
    /// start again.
    /// </summary>
    public void ResetProgress()
    {
        _messages.Reset();
    }

    public DicomDataset GetNextDatasetToProcess(out string filename, out Dictionary<string, string> otherValuesToStoreInRow)
    {

        if (!_messages.MoveNext())
        {
            filename = null;
            otherValuesToStoreInRow = null;
            return null;
        }

        Count++;

        var toReturn = _messages.Current;
        filename = toReturn?.DicomFileMessage.DicomFilePath;
        otherValuesToStoreInRow = new Dictionary<string, string>
        {
            { "MessageGuid", toReturn?.Header.MessageGuid.ToString() },
            { "DicomFileSize", toReturn?.DicomFileMessage.DicomFileSize.ToString() } //TN: It won't be a string when it hits the database but the API supports only string/string for this out Dictionary
        };
        return toReturn?.DicomDataset;
    }

    public void MarkCorrupt(DicomDataset ds)
    {
        if (_messages.Current?.DicomDataset == ds)
            CorruptMessages.Add(_messages.Current);
    }
}
