using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using System;
using System.Reflection;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    [TestFixture]
    public class MongoFileStatusDocTest
    {
        private readonly TestDateTimeProvider _dateTimeProvider = new();

        private readonly MessageHeader _messageHeader = new()
        {
            Parents = new[] { Guid.NewGuid(), },
        };

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void Test_MongoFileStatusDoc_IsIdentifiable_StatusMessage()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MongoFileStatusDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                    "input.dcm",
                    "anon.dcm",
                    true,
                    false,
                    ExtractedFileStatus.Anonymised,
                    null));
            Assert.DoesNotThrow(() =>
                new MongoFileStatusDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                    "input.dcm",
                    "anon.dcm",
                    true,
                    true,
                    ExtractedFileStatus.Anonymised,
                    null));
        }

        [Test]
        public void Test_MongoFileStatusDoc_ParseOldFormat_VerificationMessage()
        {
            // NOTE(rkm 2020-08-28) Format as of release v1.11.1
            const string jsonDoc = @"
{
    '_id' : ObjectId('5f490ef8473b9739448cbe4c'),
    'header': {
        'extractionJobIdentifier':'f9586843-8dbb-46a6-b36d-4646fdfddede',
        'messageGuid': '21a63ac3-c6f0-4fb9-973c-c97490920246',
        'producerExecutableName':'IsIdentifiable',
        'producerProcessID': 1234,
        'originalPublishTimestamp': ISODate('2020-08-28T12:00:00.000Z'),
        'parents': 'cd6430dc-952e-420e-808c-7910e61e9278->a9e16701-ef8b-482c-8b1b-023f6f40fdde->cc84ebbc-ebd0-40d0-a7da-8a2c5004b8bc',
        'receivedAt': ISODate('2020-08-28T12:00:00.000Z')
    },
    'anonymisedFileName' : 'anon.dcm',
    'wasAnonymised' : true,
    'isIdentifiable' : false,
    'statusMessage' : '[]'
}";
            var parsed = BsonSerializer.Deserialize<MongoFileStatusDoc>(BsonDocument.Parse(jsonDoc));

            Assert.AreEqual("anon.dcm", parsed.OutputFileName);
            Assert.AreEqual("<unknown>", parsed.DicomFilePath);
            Assert.AreEqual(ExtractedFileStatus.Anonymised, parsed.ExtractedFileStatus);
        }

        [Test]
        public void Test_MongoFileStatusDoc_ParseOldFormat_AnonFailedMessage()
        {
            // NOTE(rkm 2020-08-28) Format as of release v1.11.1
            const string jsonDoc = @"
{
    '_id' : ObjectId('5f490ef8473b9739448cbe4c'),
    'header': {
        'extractionJobIdentifier':'f9586843-8dbb-46a6-b36d-4646fdfddede',
        'messageGuid': '21a63ac3-c6f0-4fb9-973c-c97490920246',
        'producerExecutableName':'CTPAnonymiser',
        'producerProcessID': 1234,
        'originalPublishTimestamp': ISODate('2020-08-28T12:00:00.000Z'),
        'parents': 'cd6430dc-952e-420e-808c-7910e61e9278->a9e16701-ef8b-482c-8b1b-023f6f40fdde',
        'receivedAt': ISODate('2020-08-28T12:00:00.000Z')
    },
    'anonymisedFileName' : null,
    'wasAnonymised' : false,
    'isIdentifiable' : false,
    'statusMessage' : 'failed to anonymise'
}";
            var parsed = BsonSerializer.Deserialize<MongoFileStatusDoc>(BsonDocument.Parse(jsonDoc));

            Assert.AreEqual(null, parsed.OutputFileName);
            Assert.AreEqual("<unknown>", parsed.DicomFilePath);
            Assert.AreEqual(ExtractedFileStatus.ErrorWontRetry, parsed.ExtractedFileStatus);
        }

        [Test]
        public void TestMongoFileStatusDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoFileStatusDoc).GetProperties())
                Assert.True(p.CanWrite, $"Property '{p.Name}' is not writeable");
        }

        [Test]
        public void TestMongoFileStatusDoc_Equality()
        {
            Guid guid = Guid.NewGuid();
            var doc1 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "input.dcm",
                "anon.dcm",
                true,
                false,
                ExtractedFileStatus.Anonymised,
                "anonymised");

            var doc2 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "input.dcm",
                "anon.dcm",
                true,
                false,
                ExtractedFileStatus.Anonymised,
                "anonymised");

            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoFileStatusDoc_GetHashCode()
        {
            Guid guid = Guid.NewGuid();
            var doc1 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "input.dcm",
                "anon.dcm",
                true,
                false,
                ExtractedFileStatus.Anonymised,
                "anonymised");

            var doc2 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "input.dcm",
                "anon.dcm",
                true,
                false,
                ExtractedFileStatus.Anonymised,
                "anonymised");

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        #endregion
    }
}
