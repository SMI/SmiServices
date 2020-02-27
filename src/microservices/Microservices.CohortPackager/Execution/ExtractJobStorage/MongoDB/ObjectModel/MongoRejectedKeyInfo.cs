using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    /// <summary>
    /// MongoDB document model representing the rejection reasons for a specific key
    /// </summary>
    public class MongoRejectedKeyInfo
    {
        [BsonElement("header")]
        public ExtractFileCollectionHeader Header { get; set; }

        [BsonElement("key")]
        public string Key { get; set; }

        [BsonElement("rejectionInfo")]
        public Dictionary<string, int> RejectionInfo { get; set; }

        #region Equality Members

        protected bool Equals(MongoRejectedKeyInfo other)
        {
            return Equals(Header, other.Header) &&
                   Key == other.Key &&
                   RejectionInfo.OrderBy(x => x.Key).SequenceEqual(other.RejectionInfo.OrderBy(x => x.Key));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MongoRejectedKeyInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Header != null ? Header.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RejectionInfo != null ? RejectionInfo.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(MongoRejectedKeyInfo left, MongoRejectedKeyInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MongoRejectedKeyInfo left, MongoRejectedKeyInfo right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}