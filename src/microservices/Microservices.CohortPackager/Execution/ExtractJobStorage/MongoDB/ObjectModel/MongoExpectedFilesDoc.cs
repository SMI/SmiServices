using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    /// <summary>
    /// MongoDB document model representing a set of files which are expected to be extracted
    /// </summary>
    [BsonIgnoreExtraElements]
    public class MongoExpectedFilesDoc : IEquatable<MongoExpectedFilesDoc>
    {
        [BsonElement("header")]
        [NotNull]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("key")]
        [NotNull]
        public string Key { get; set; }

        [BsonElement("expectedFiles")]
        [NotNull]
        public HashSet<MongoExpectedFileInfoDoc> ExpectedFiles { get; set; }

        [BsonElement("rejectedKeys")]
        [NotNull]
        public MongoRejectedKeyInfoDoc RejectedKeys { get; set; }


        public MongoExpectedFilesDoc(
            [NotNull] MongoExtractionMessageHeaderDoc header,
            [NotNull] string key,
            [NotNull] HashSet<MongoExpectedFileInfoDoc> expectedFiles,
            [NotNull] MongoRejectedKeyInfoDoc rejectedKeys)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Key = (!string.IsNullOrWhiteSpace(key)) ? key : throw new ArgumentNullException(nameof(key));
            ExpectedFiles = expectedFiles ?? throw new ArgumentNullException(nameof(expectedFiles));
            RejectedKeys = rejectedKeys ?? throw new ArgumentNullException(nameof(rejectedKeys));
        }

        public static MongoExpectedFilesDoc FromMessage(
            [NotNull] ExtractFileCollectionInfoMessage message,
            [NotNull] IMessageHeader header,
            [NotNull] DateTimeProvider dateTimeProvider)
        {
            return new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(message.ExtractionJobIdentifier, header, dateTimeProvider),
                message.KeyValue,
                new HashSet<MongoExpectedFileInfoDoc>(message.ExtractFileMessagesDispatched.Select(x => new MongoExpectedFileInfoDoc(x.Key.MessageGuid, x.Value))),
                MongoRejectedKeyInfoDoc.FromMessage(message, header, dateTimeProvider));
        }

        #region Equality Members

        public bool Equals(MongoExpectedFilesDoc other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Header, other.Header) &&
                   Key == other.Key &&
                   ExpectedFiles.SequenceEqual(other.ExpectedFiles) &&
                   Equals(RejectedKeys, other.RejectedKeys);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MongoExpectedFilesDoc)obj);
        }

        public static bool operator ==(MongoExpectedFilesDoc a, MongoExpectedFilesDoc b) => Equals(a, b);

        public static bool operator !=(MongoExpectedFilesDoc a, MongoExpectedFilesDoc b) => !Equals(a, b);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Header.GetHashCode();
                hashCode = (hashCode * 397) ^ Key.GetHashCode();
                hashCode = (hashCode * 397) ^ ExpectedFiles.GetHashCode();
                hashCode = (hashCode * 397) ^ (RejectedKeys != null ? RejectedKeys.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }

    public class MongoExpectedFileInfoDoc : IEquatable<MongoExpectedFileInfoDoc>
    {
        [BsonElement("extractFileMessageGuid")]
        [BsonRepresentation(BsonType.String)]
        public Guid ExtractFileMessageGuid { get; set; }

        [BsonElement("anonymisedFilePath")]
        [NotNull]
        public string AnonymisedFilePath { get; set; }

        public MongoExpectedFileInfoDoc(
            Guid extractFileMessageGuid,
            [NotNull] string anonymisedFilePath)
        {
            ExtractFileMessageGuid = (extractFileMessageGuid != default(Guid)) ? extractFileMessageGuid : throw new ArgumentException(nameof(extractFileMessageGuid));
            AnonymisedFilePath = (!string.IsNullOrWhiteSpace(anonymisedFilePath)) ? anonymisedFilePath : throw new ArgumentException(nameof(anonymisedFilePath));
        }

        #region Equality Members

        public bool Equals(MongoExpectedFileInfoDoc other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ExtractFileMessageGuid.Equals(other.ExtractFileMessageGuid) &&
                   AnonymisedFilePath == other.AnonymisedFilePath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MongoExpectedFileInfoDoc)obj);
        }

        public static bool operator ==(MongoExpectedFileInfoDoc a, MongoExpectedFileInfoDoc b) => Equals(a, b);

        public static bool operator !=(MongoExpectedFileInfoDoc a, MongoExpectedFileInfoDoc b) => !Equals(a, b);

        public override int GetHashCode()
        {
            unchecked
            {
                return (ExtractFileMessageGuid.GetHashCode() * 397) ^ AnonymisedFilePath.GetHashCode();
            }
        }

        #endregion
    }

    /// <summary>
    /// MongoDB document model representing the rejection reasons for a specific key
    /// </summary>
    public class MongoRejectedKeyInfoDoc
    {
        [BsonElement("header")]
        [NotNull]
        public MongoExtractionMessageHeaderDoc Header { get; set; }

        [BsonElement("rejectionInfo")]
        [NotNull]
        public Dictionary<string, int> RejectionInfo { get; set; }

        public MongoRejectedKeyInfoDoc(
            [NotNull] MongoExtractionMessageHeaderDoc header,
            [NotNull] Dictionary<string, int> rejectionInfo)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            RejectionInfo = rejectionInfo ?? throw new ArgumentNullException(nameof(rejectionInfo));
        }

        public static MongoRejectedKeyInfoDoc FromMessage(
            [NotNull] ExtractFileCollectionInfoMessage message,
            [NotNull] IMessageHeader header,
            [NotNull] DateTimeProvider dateTimeProvider)
        {
            return new MongoRejectedKeyInfoDoc(
                 MongoExtractionMessageHeaderDoc.FromMessageHeader(message.ExtractionJobIdentifier, header, dateTimeProvider),
                 message.RejectionReasons
            );
        }

        #region Equality Members

        protected bool Equals(MongoRejectedKeyInfoDoc other)
        {
            return Equals(Header, other.Header) &&
                   RejectionInfo.OrderBy(x => x.Key).SequenceEqual(other.RejectionInfo.OrderBy(x => x.Key));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MongoRejectedKeyInfoDoc)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Header.GetHashCode();
                hashCode = (hashCode * 397) ^ RejectionInfo.GetHashCode();
                return hashCode;
            }
        }

        #endregion
    }
}
