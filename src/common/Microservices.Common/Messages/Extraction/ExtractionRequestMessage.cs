using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microservices.Common.Messages.Extraction
{
    /// <summary>
    /// Describes a request to extract all images identified by a DicomTag e.g. SeriesInstanceUID with the specified project specific patient identifiers (PatientID)
    /// </summary>
    public class ExtractionRequestMessage : ExtractMessage, IEquatable<ExtractionRequestMessage>
    {
        /// <summary>
        /// Contains the name of the identifier you want to extract based on (this should be a DicomTag e.g. 'SeriesInstanceUID')
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string KeyTag { get; set; }

        /// <summary>
        /// The unique set of identifiers of Type <see cref="KeyTag"/> which should be extracted
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<string> ExtractionIdentifiers { get; set; }
        
        [JsonConstructor]
        public ExtractionRequestMessage()
        {
            ExtractionIdentifiers = new List<string>();
        }

        #region Equality Members

        public bool Equals(ExtractionRequestMessage other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) &&
                   string.Equals(KeyTag, other.KeyTag) &&
                   ExtractionIdentifiers.Count == other.ExtractionIdentifiers.Count &&
                   !ExtractionIdentifiers.Except(other.ExtractionIdentifiers).Any();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractionRequestMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (KeyTag != null ? KeyTag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ExtractionIdentifiers != null ? ExtractionIdentifiers.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
