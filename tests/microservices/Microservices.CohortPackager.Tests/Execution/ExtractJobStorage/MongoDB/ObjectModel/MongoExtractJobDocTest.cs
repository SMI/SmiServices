using System;
using System.Reflection;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;


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

        private class TestException : Exception
        {
            public override string StackTrace { get; } = "StackTrace";

            public TestException(string message) : base(message) { }
        }

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
                null);

            Assert.AreEqual(expected, doc);
        }

        [Test]
        public void TestMongoExtractJobDoc_Equality()
        {
            Guid guid = Guid.NewGuid();
            var exception = new TestException("aaah");
            var failedInfoDoc = new MongoFailedJobInfoDoc(exception, _dateTimeProvider);

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
