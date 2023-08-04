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

        private void AssertDocsEqualExceptHeader(MongoFileStatusDoc expected, MongoFileStatusDoc actual)
        {
            actual.ExtraElements = null;

            foreach (PropertyInfo prop in expected.GetType().GetProperties())
            {
                if (prop.Name == "Header")
                    continue;

                var expectedProp = prop.GetValue(expected);
                var parsedProp = prop.GetValue(actual);
                Assert.AreEqual(expectedProp, parsedProp);
            }
        }

        #endregion

        #region Tests

        [Test]
        public void Test_MongoFileStatusDoc_IsIdentifiable_StatusMessage()
        {
            var exc = Assert.Throws<ArgumentException>(() =>
                new MongoFileStatusDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                    "input.dcm",
                    "anon.dcm",
                    ExtractedFileStatus.Anonymised,
                    VerifiedFileStatus.NotIdentifiable,
                    null
                )
            );
            Assert.AreEqual("Cannot be null or whitespace except for successful file copies (Parameter 'statusMessage')", exc!.Message);

            exc = Assert.Throws<ArgumentException>(() =>
                new MongoFileStatusDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                    "input.dcm",
                    "anon.dcm",
                    ExtractedFileStatus.Anonymised,
                    VerifiedFileStatus.NotIdentifiable,
                    "  "
                )
            );
            Assert.AreEqual("Cannot be null or whitespace except for successful file copies (Parameter 'statusMessage')", exc!.Message);

            var _ = new MongoFileStatusDoc(
                   MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                   "input.dcm",
                   "anon.dcm",
                   ExtractedFileStatus.Copied,
                   VerifiedFileStatus.NotVerified,
                   "  "
            );
        }

        [Test]
        public void ParseVerificationMessage_v1_11_1()
        {
            // Arrange

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

            var expected = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                "<unknown>",
                "anon.dcm",
                ExtractedFileStatus.Anonymised,
                VerifiedFileStatus.NotIdentifiable,
                "[]"
            );

            // Act

            var parsed = BsonSerializer.Deserialize<MongoFileStatusDoc>(BsonDocument.Parse(jsonDoc));

            // Assert

            AssertDocsEqualExceptHeader(expected, parsed);
        }

        [Test]
        public void ParseAnonFailedMessage_v1_11_1()
        {
            // Arrange

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

            var expected = new MongoFileStatusDoc(
              MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
              "<unknown>",
              null,
              ExtractedFileStatus.ErrorWontRetry,
              VerifiedFileStatus.NotVerified,
              "failed to anonymise"
            );

            // Act

            var parsed = BsonSerializer.Deserialize<MongoFileStatusDoc>(BsonDocument.Parse(jsonDoc));

            // Assert

            AssertDocsEqualExceptHeader(expected, parsed);
        }

        [Test]
        public void ParseAnonFailedMessage_v5_1_3()
        {
            // Arrange

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
    'dicomFilePath'  : 'foo.dcm'
    'outputFileName' : null,
    'wasAnonymised' : false,
    'isIdentifiable' : false,
    'extractedFileStatus' : 'ErrorWontRetry'
    'statusMessage' : 'failed to anonymise'
}";

            var expected = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                "foo.dcm",
                null,
                ExtractedFileStatus.ErrorWontRetry,
                VerifiedFileStatus.NotVerified,
                "failed to anonymise"
            );

            // Act

            var parsed = BsonSerializer.Deserialize<MongoFileStatusDoc>(BsonDocument.Parse(jsonDoc));

            // Assert

            AssertDocsEqualExceptHeader(expected, parsed);
        }

        [Test]
        public void ParseVerificationMessage_v5_1_3()
        {
            // Arrange

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
    'dicomFilePath'  : 'foo.dcm'
    'outputFileName' : 'foo-an.dcm',
    'wasAnonymised' : true,
    'isIdentifiable' : false,
    'extractedFileStatus' : 'Anonymised'
    'statusMessage' : '[]'
}";

            var expected = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader(), new DateTimeProvider()),
                "foo.dcm",
                "foo-an.dcm",
                ExtractedFileStatus.Anonymised,
                VerifiedFileStatus.NotIdentifiable,
                "[]"
            );

            // Act

            var parsed = BsonSerializer.Deserialize<MongoFileStatusDoc>(BsonDocument.Parse(jsonDoc));

            // Assert

            AssertDocsEqualExceptHeader(expected, parsed);
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
                ExtractedFileStatus.Anonymised,
                VerifiedFileStatus.NotIdentifiable,
                "anonymised");

            var doc2 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "input.dcm",
                "anon.dcm",
                ExtractedFileStatus.Anonymised,
                VerifiedFileStatus.NotIdentifiable,
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
                ExtractedFileStatus.Anonymised,
                VerifiedFileStatus.NotIdentifiable,
                "anonymised");

            var doc2 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "input.dcm",
                "anon.dcm",
                ExtractedFileStatus.Anonymised,
                VerifiedFileStatus.NotIdentifiable,
                "anonymised");

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        #endregion
    }
}
