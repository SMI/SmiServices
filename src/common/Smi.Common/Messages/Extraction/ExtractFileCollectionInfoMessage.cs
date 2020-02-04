
using Smi.Common.MessageSerialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Smi.Common.Messages.Extraction
{
    /// <summary>
    /// Describes all the <see cref="ExtractFileMessage"/> sent for a single key Tag (e.g. SeriesInstanceUID) value provided by <see cref="ExtractionRequestMessage"/> 
    /// (i.e. a single entry in <see cref="ExtractionRequestMessage.ExtractionIdentifiers"/>).
    /// </summary>
    public class ExtractFileCollectionInfoMessage : ExtractMessage, IEquatable<ExtractFileCollectionInfoMessage>
    {
        /// <summary>
        /// Contains the value of the Tag <see cref="KeyTag"/> which is being extracted
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string KeyValue { get; set; }

        /// <summary>
        /// Collection of all the messages sent out as the result of an <see cref="ExtractionRequestMessage"/> (headers only) along with the file path extracted
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public JsonCompatibleDictionary<MessageHeader, string> ExtractFileMessagesDispatched { get; set; }
        
        /// <summary>
        /// All the reasons for message rejection and count of occurrences
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public Dictionary<string, int> RejectionReasons { get; set; } = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);


        [JsonConstructor]
        public ExtractFileCollectionInfoMessage()
        {
            ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>();
        }
        
        public ExtractFileCollectionInfoMessage(Guid extractionJobIdentifier, string projectNumber, string extractionDirectory, DateTime jobSubmittedAt)
            : base(extractionJobIdentifier, projectNumber, extractionDirectory, jobSubmittedAt) { }

        public ExtractFileCollectionInfoMessage(ExtractionRequestMessage request)
            : base(request)
        {
            ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>();
        }

        #region Equality Members

        public bool Equals(ExtractFileCollectionInfoMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) && Equals(ExtractFileMessagesDispatched, other.ExtractFileMessagesDispatched);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ExtractFileCollectionInfoMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (KeyValue != null ? KeyValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ExtractFileMessagesDispatched != null ? ExtractFileMessagesDispatched.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
