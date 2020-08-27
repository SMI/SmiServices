using Microservices.CohortPackager.Execution.ExtractJobStorage;
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
    public class MongoExtractJobDocTest
    {
        private readonly DateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        private readonly MessageHeader _messageHeader = new MessageHeader
        {
            Parents = new[] { Guid.NewGuid() }
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
        public void TestMongoExtractJobDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoExtractJobDoc).GetProperties())
                Assert.True(p.CanWrite, $"Property '{p.Name}' is not writeable");
        }

        [Test]
        public void TestMongoExtractJobDoc_FromMessage()
        {
            Guid guid = Guid.NewGuid();
            var message = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "1234",
                ExtractionJobIdentifier = guid,
                ExtractionDirectory = "test/directory",
                KeyTag = "KeyTag",
                KeyValueCount = 123,
            };

            MongoExtractJobDoc doc = MongoExtractJobDoc.FromMessage(message, _messageHeader, _dateTimeProvider);

            var expected = new MongoExtractJobDoc(
                guid,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "1234",
                ExtractJobStatus.WaitingForCollectionInfo,
                "test/directory",
                _dateTimeProvider.UtcNow(),
                "KeyTag",
                123,
                "MR",
                isIdentifiableExtraction: false,
                null);

            Assert.AreEqual(expected, doc);
        }

        [Test]
        public void TestMongoExtractJobDoc_Equality()
        {
            Guid guid = Guid.NewGuid();
            var failedInfoDoc = new MongoFailedJobInfoDoc(new TestException("aaah"), _dateTimeProvider);

            var doc1 = new MongoExtractJobDoc(
                guid,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "1234",
                ExtractJobStatus.WaitingForCollectionInfo,
                "test/directory",
                _dateTimeProvider.UtcNow(),
                "KeyTag",
                123,
                "MR",
                isIdentifiableExtraction: false,
                failedInfoDoc);
            var doc2 = new MongoExtractJobDoc(
                guid,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "1234",
                ExtractJobStatus.WaitingForCollectionInfo,
                "test/directory",
                _dateTimeProvider.UtcNow(),
                "KeyTag",
                123,
                "MR",
                isIdentifiableExtraction: false,
                failedInfoDoc);

            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoExtractJobDoc_GetHashCode()
        {
            Guid guid = Guid.NewGuid();

            var doc1 = new MongoExtractJobDoc(
                guid,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "1234",
                ExtractJobStatus.WaitingForCollectionInfo,
                "test/directory",
                _dateTimeProvider.UtcNow(),
                "KeyTag",
                123,
                "MR",
                isIdentifiableExtraction: false,
                null);
            var doc2 = new MongoExtractJobDoc(
                guid,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "1234",
                ExtractJobStatus.WaitingForCollectionInfo,
                "test/directory",
                _dateTimeProvider.UtcNow(),
                "KeyTag",
                123,
                "MR",
                isIdentifiableExtraction: false,
                null);

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        [Test]
        public void TestMongoExtractJobDoc_IsIdentifiableExtraction_MissingValueOk()
        {
            // TODO(rkm 2020-08-27) This works by chance since the missing boolean value defaults to false anyway. Need to think of a better way of handling this kind of backwards compatibility
            var jsonDoc = "{ \"_id\" : \"0fbd4893-c116-4f16-88c8-f7084531d87c\", \"header\" : { \"extractionJobIdentifier\" : \"0fbd4893-c116-4f16-88c8-f7084531d87c\", \"messageGuid\" : \"23475d89-6e2c-431c-bc5d-7b7c25ffb6a0\", \"producerExecutableName\" : \"testhost\", \"producerProcessID\" : 14372, \"originalPublishTimestamp\" : { \"$date\" : 1598528178000 }, \"parents\" : \"30603fb0-3bec-43de-8ba8-db55a029c664\", \"receivedAt\" : { \"$date\" : 1598531778957 } }, \"projectNumber\" : \"1234\", \"jobStatus\" : \"WaitingForCollectionInfo\", \"extractionDirectory\" : \"test/directory\", \"jobSubmittedAt\" : { \"$date\" : 1598531778957 }, \"keyTag\" : \"KeyTag\", \"keyCount\" : 123, \"extractionModality\" : \"MR\",\"failedJobInfo\" : null }";
            BsonDocument bsonDoc = BsonDocument.Parse(jsonDoc);
            var mongoExtractJobDoc = BsonSerializer.Deserialize<MongoExtractJobDoc>(bsonDoc);
            Assert.False(mongoExtractJobDoc.IsIdentifiableExtraction);
        }
        
        [Test]
        public void TestMongoFailedJobInfoDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoFailedJobInfoDoc).GetProperties())
                Assert.True(p.CanWrite, $"Property '{p.Name}' is not writeable");
        }

        [Test]
        public void TestMongoFailedJobInfo_Equality()
        {
            var exception = new TestException("aaah");

            var doc1 = new MongoFailedJobInfoDoc(exception, _dateTimeProvider);
            var doc2 = new MongoFailedJobInfoDoc(exception, _dateTimeProvider);

            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoFailedJobInfo_GetHashCode()
        {
            var exception = new TestException("aaah");

            var doc1 = new MongoFailedJobInfoDoc(exception, _dateTimeProvider);
            var doc2 = new MongoFailedJobInfoDoc(exception, _dateTimeProvider);

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        #endregion
    }
}
