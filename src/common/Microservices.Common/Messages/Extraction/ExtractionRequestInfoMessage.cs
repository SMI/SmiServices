
using Newtonsoft.Json;
using System;

namespace Microservices.Common.Messages.Extraction
{
    public class ExtractionRequestInfoMessage : ExtractMessage, IEquatable<ExtractionRequestInfoMessage>
    {
        [JsonProperty(Required = Required.Always)]
        public string KeyTag { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int KeyValueCount { get; set; }


        [JsonConstructor]
        public ExtractionRequestInfoMessage() { }


        #region Equality Members

        public bool Equals(ExtractionRequestInfoMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) &&
                   string.Equals(KeyTag, other.KeyTag) &&
                   KeyValueCount == other.KeyValueCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractionRequestInfoMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (KeyTag != null ? KeyTag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ KeyValueCount;
                return hashCode;
            }
        }

        public static bool operator ==(ExtractionRequestInfoMessage left, ExtractionRequestInfoMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtractionRequestInfoMessage left, ExtractionRequestInfoMessage right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
