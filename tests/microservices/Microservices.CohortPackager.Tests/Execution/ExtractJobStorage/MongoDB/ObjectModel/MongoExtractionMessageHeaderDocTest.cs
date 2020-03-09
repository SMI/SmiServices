using System;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    [TestFixture]
    public class MongoExtractionMessageHeaderDocTest
    {
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
        public void TestMongoExtractionMessageHeaderDoc_FromMessageHeader()
        {
            Guid guid = Guid.NewGuid();
            Guid p1 = Guid.NewGuid();
            Guid p2 = Guid.NewGuid();
            const long unixTimeNow = 1234;
            var dateTimeProvider = new TestDateTimeProvider();

            var header = new MessageHeader
            {
                MessageGuid = guid,
                ProducerExecutableName = "TestFromMessageHeader",
                ProducerProcessID = 1234,
                Parents = new[] { p1, p2 },
                OriginalPublishTimestamp = unixTimeNow,
            };

            MongoExtractionMessageHeaderDoc doc = MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, header, dateTimeProvider);

            var expected = new MongoExtractionMessageHeaderDoc(
                guid,
                guid,
                "TestFromMessageHeader",
                1234,
                DateTime.UnixEpoch + TimeSpan.FromSeconds(unixTimeNow),
                $"{p1}->{p2}",
                dateTimeProvider.UtcNow()
            );

            Assert.AreEqual(expected, doc);
        }

        [Test]
        public void TestMongoExtractionMessageHeaderDoc_Equality()
        {
            Guid guid = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            var doc1 = new MongoExtractionMessageHeaderDoc(guid, guid, "Test1", 123, now, "parents", now);
            var doc2 = new MongoExtractionMessageHeaderDoc(guid, guid, "Test1", 123, now, "parents", now);

            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoExtractionMessageHeaderDoc_GetHashCode()
        {

            Guid guid = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            var doc1 = new MongoExtractionMessageHeaderDoc(guid, guid, "Test1", 123, now, "parents", now);
            var doc2 = new MongoExtractionMessageHeaderDoc(guid, guid, "Test1", 123, now, "parents", now);

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        #endregion
    }
}
