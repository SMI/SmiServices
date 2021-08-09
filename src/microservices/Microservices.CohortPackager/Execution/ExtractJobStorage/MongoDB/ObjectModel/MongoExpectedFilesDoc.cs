using Equ;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    /// <summary>
    /// MongoDB document model representing a set of files which are expected to be extracted
    /// </summary>
    [BsonIgnoreExtraElements] // NOTE(rkm 2020-08-28) Required for classes which don't contain a field marked with BsonId
    public class MongoExpectedFilesDoc : MemberwiseEquatable<MongoExpectedFilesDoc>
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
    }

    public class MongoExpectedFileInfoDoc : MemberwiseEquatable<MongoExpectedFileInfoDoc>
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
    }

    /// <summary>
    /// MongoDB document model representing the rejection reasons for a specific key
    /// </summary>
    public class MongoRejectedKeyInfoDoc : MemberwiseEquatable<MongoRejectedKeyInfoDoc>
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
    }
}
