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

        private readonly MessageHeader _messageHeader = new()
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
        public void Test_MongoExtractJobDoc_CopyConstructor()
        {
            var jobid = Guid.NewGuid();
            var original = new MongoExtractJobDoc(
                jobid,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobid, _messageHeader, _dateTimeProvider),
                "1234",
                ExtractJobStatus.WaitingForCollectionInfo,
                "test/directory",
                _dateTimeProvider.UtcNow(),
                "KeyTag",
                123,
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true,
                new MongoFailedJobInfoDoc(new Exception("foo"), _dateTimeProvider)
            );
            var copied = new MongoExtractJobDoc(original);

            Assert.AreEqual(original, copied);
        }


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
                UserName = "testUser",
                IsIdentifiableExtraction = true,
                IsNoFilterExtraction = true,
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
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true,
                null);

            Assert.AreEqual(expected, doc);
        }

        [Test]
        public void TestMongoExtractJobDoc_Parse_v5_4_0()
        {
            const string jsonDoc = @"
{
    _id: '898a207b-cc2a-4014-97f0-f881c07a3d65',
    header: {
      extractionJobIdentifier: '898a207b-cc2a-4014-97f0-f881c07a3d65',
      messageGuid: '613aefff-1714-4913-9b8a-ebe2d09bb590',
      producerExecutableName: 'smi',
      producerProcessID: 15443,
      originalPublishTimestamp: ISODate('2023-11-08T13:14:08.000Z'),
      parents: '',
      receivedAt: ISODate('2023-11-08T13:14:09.019Z')
    },
    projectNumber: '1337',
    jobStatus: 'WaitingForCollectionInfo',
    extractionDirectory: '1337/extractions/DicomFiles',
    jobSubmittedAt: ISODate('2023-11-08T13:14:06.881Z'),
    keyTag: 'StudyInstanceUID',
    keyCount: 10,
    extractionModality: null,
    isIdentifiableExtraction: false,
    IsNoFilterExtraction: false,
    failedJobInfo: null
}";
            var mongoExtractJobDoc = BsonSerializer.Deserialize<MongoExtractJobDoc>(BsonDocument.Parse(jsonDoc));
            Assert.Null(mongoExtractJobDoc.UserName);
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
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true,
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
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true,
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
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true,
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
                "testUser",
                "MR",
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true,
                null);

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
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
