using Newtonsoft.Json;

namespace SmiServices.Common.Messages.Extraction;

/// <summary>
/// Status message sent by services which extract files (CTP, FileCopier)
/// </summary>
public class ExtractedFileStatusMessage : ExtractMessage, IFileReferenceMessage
{
    /// <summary>
    /// Original file path
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string DicomFilePath { get; set; } = null!;

    /// <summary>
    /// The <see cref="ExtractedFileStatus"/> for this file
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public ExtractedFileStatus Status { get; set; }

    /// <summary>
    /// Output file path, relative to the extraction directory. Only required if an output file has been produced
    /// </summary>
    [JsonProperty(Required = Required.AllowNull)]
    public string? OutputFilePath { get; set; }

    /// <summary>
    /// Message required if Status is not 0
    /// </summary>
    [JsonProperty(Required = Required.AllowNull)]
    public string? StatusMessage { get; set; }


    [JsonConstructor]
    public ExtractedFileStatusMessage() { }

    public ExtractedFileStatusMessage(ExtractFileMessage request)
        : base(request)
    {
        DicomFilePath = request.DicomFilePath;
        OutputFilePath = request.OutputPath;
    }

    public override string ToString() =>
        $"{base.ToString()}," +
        $"DicomFilePath={DicomFilePath}," +
        $"ExtractedFileStatus={Status}," +
        $"OutputFilePath={OutputFilePath}," +
        $"StatusMessage={StatusMessage}," +
        "";
}
