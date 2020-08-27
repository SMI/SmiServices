using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
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
        private readonly TestDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        private readonly MessageHeader _messageHeader = new MessageHeader
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
