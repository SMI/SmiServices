using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    public class ExpectedAnonymisedFileInfo
    {
        [BsonElement("extractFileMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractFileMessageGuid { get; set; }

        [BsonElement("anonymisedFilePath")]
        public string AnonymisedFilePath { get; set; }

        protected bool Equals(ExpectedAnonymisedFileInfo other)
        {
            return ExtractFileMessageGuid.Equals(other.ExtractFileMessageGuid) && AnonymisedFilePath == other.AnonymisedFilePath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExpectedAnonymisedFileInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ExtractFileMessageGuid.GetHashCode() * 397) ^ (AnonymisedFilePath != null ? AnonymisedFilePath.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ExpectedAnonymisedFileInfo left, ExpectedAnonymisedFileInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExpectedAnonymisedFileInfo left, ExpectedAnonymisedFileInfo right)
        {
            return !Equals(left, right);
        }
    }
}