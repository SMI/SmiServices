using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Smi.Common.Messages;
using System.Text.Json.Serialization;
using IsIdentifiable.Reporting;
using Smi.Common.Messages.Extraction;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Smi.Common.MessageSerialization;

/// <summary>
/// Helper class to (de)serialize objects from RabbitMQ messages.
/// </summary>
public static class JsonConvert
{
    private static readonly Dictionary<Type, System.Text.Json.Serialization.Metadata.JsonTypeInfo> contexts = new()
    {
        {typeof(ExtractedFileStatusMessage),JsonContext.Default.ExtractedFileStatusMessage},
        {typeof(ExtractFileCollectionInfoMessage),JsonContext.Default.ExtractFileCollectionInfoMessage},
        {typeof(ExtractionRequestMessage),JsonContext.Default.ExtractionRequestMessage},
        {typeof(IEnumerable<Failure>),JsonContext.Default.FailureList}
    };
    
    /// <summary>
    /// Deserialize a message from a string.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IMessage"/> to deserialize into.</typeparam>
    /// <param name="message">The message to deserialize.</param>
    /// <returns></returns>
    public static T DeserializeObject<T>(ReadOnlySpan<byte> message) where T : class, IMessage
    {
        if (!contexts.TryGetValue(typeof(T), out var context))
            throw new ArgumentException($"No JSON conversion for IMessage type {typeof(T)}");
        if (JsonSerializer.Deserialize(message, typeof(T), JsonContext.Default) is not T messageObj)
            throw new ApplicationException("Deserialized message object is null, message was empty.");
        return messageObj;
    }

    public static T DeserializeObject<T>(string s) where T : class, IMessage
    {
        return DeserializeObject<T>(Encoding.UTF8.GetBytes(s).ToArray());
    }

}

internal class FailureList : List<Failure> {

}

[JsonSerializable(typeof(ExtractedFileStatusMessage))]
[JsonSerializable(typeof(ExtractFileCollectionInfoMessage))]
[JsonSerializable(typeof(ExtractionRequestMessage))]
[JsonSerializable(typeof(FailureList))]
internal partial class JsonContext : JsonSerializerContext
{

}