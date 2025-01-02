using Newtonsoft.Json;
using SmiServices.Common.MessageSerialization;
using System;
using System.Collections.Generic;

namespace SmiServices.Common.Messages.Extraction;

/// <summary>
/// Describes all the <see cref="ExtractFileMessage"/> sent for a single key Tag (e.g. SeriesInstanceUID) value provided by <see cref="ExtractionRequestMessage"/> 
/// (i.e. a single entry in <see cref="ExtractionRequestMessage.ExtractionIdentifiers"/>).
/// </summary>
public class ExtractFileCollectionInfoMessage : ExtractMessage
{
    /// <summary>
    /// Contains the value of the tag which is being extracted
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public string KeyValue { get; set; } = null!;

    /// <summary>
    /// Collection of all the messages sent out as the result of an <see cref="ExtractionRequestMessage"/> (headers only) along with the file path extracted
    /// </summary>
    [JsonProperty(Required = Required.Always)]
    public JsonCompatibleDictionary<MessageHeader, string> ExtractFileMessagesDispatched { get; set; } = null!;

    /// <summary>
    /// All the reasons for message rejection and count of occurrences
    /// </summary>
    [JsonProperty(Required = Required.Default)]
    public Dictionary<string, int> RejectionReasons { get; set; } = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);


    [JsonConstructor]
    public ExtractFileCollectionInfoMessage()
    {
        ExtractFileMessagesDispatched = [];
    }

    public ExtractFileCollectionInfoMessage(ExtractionRequestMessage request)
        : base(request)
    {
        ExtractFileMessagesDispatched = [];
    }

    public override string ToString()
    {
        return base.ToString() + $",KeyValue={KeyValue},ExtractFileMessagesDispatched={ExtractFileMessagesDispatched.Count},RejectionReasons={RejectionReasons.Count},";
    }
}
