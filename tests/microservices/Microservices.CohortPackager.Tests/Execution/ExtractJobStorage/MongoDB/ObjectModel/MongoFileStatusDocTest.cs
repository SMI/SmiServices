using System;
using System.Reflection;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Tests;


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
                "anon.dcm",
                false,
                "anonymised");

            var doc2 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "anon.dcm",
                false,
                "anonymised");

            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoFileStatusDoc_GetHashCode()
        {
            Guid guid = Guid.NewGuid();
            var doc1 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "anon.dcm",
                false,
                "anonymised");

            var doc2 = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, _messageHeader, _dateTimeProvider),
                "anon.dcm",
                false,
                "anonymised");

            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        #endregion
    }
}
