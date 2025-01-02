namespace SmiServices.Common.Messages;

/// <summary>
/// Describes an IMessage that references a dicom file in physical storage
/// </summary>
public interface IFileReferenceMessage : IMessage
{
    /// <summary>
    /// File path relative to the FileSystemRoot
    /// </summary>
    string DicomFilePath { get; set; }
}
