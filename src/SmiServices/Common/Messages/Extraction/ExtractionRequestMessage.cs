using Newtonsoft.Json;
using System.Collections.Generic;

namespace SmiServices.Common.Messages.Extraction;

/// <summary>
/// Describes a request to extract all images identified by a DicomTag e.g. SeriesInstanceUID with the specified project specific patient identifiers (PatientID)
/// </summary>
public class ExtractionRequestMessage : ExtractMessage
{
    /// <summary>
    /// Contains the name of the identifier you want to extract based on (this should be a DicomTag e.g. 'SeriesInstanceUID')
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string KeyTag { get; set; } = null!;

    /// <summary>
    /// The unique set of identifiers of Type <see cref="KeyTag"/> which should be extracted
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public List<string> ExtractionIdentifiers { get; set; } = null!;

    [JsonConstructor]
    public ExtractionRequestMessage()
    {
        ExtractionIdentifiers = [];
    }

    /// <summary>
    /// (Shallow) copy constructor
    /// </summary>
    /// <param name="other"></param>
    public ExtractionRequestMessage(ExtractionRequestMessage other)
    : base(other)
    {
        KeyTag = other.KeyTag;
        ExtractionIdentifiers = other.ExtractionIdentifiers;
    }

    public override string ToString()
        => base.ToString() + ", " +
           $"KeyTag={KeyTag}, " +
           $"nIdentifiers={ExtractionIdentifiers.Count}";
}
