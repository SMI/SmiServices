using System;
using System.Reflection;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Tests;

namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    [TestFixture]
    public class MongoCompletedExtractJobDocTest
    {
        private static readonly DateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        private readonly MongoExtractJobDoc _testExtractJobDoc = new MongoExtractJobDoc(
            Guid.NewGuid(),
            MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader { Parents = new[] { Guid.NewGuid() } }, _dateTimeProvider),
            "1234",
            ExtractJobStatus.ReadyForChecks,
            "test",
            DateTime.UtcNow,
            "test",
            1,
            null,
            null);

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
        public void TestMongoCompletedExtractJobDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoCompletedExtractJobDoc).GetProperties())
                Assert.True(p.CanWrite, $"Property '{p.Name}' is not writeable");
        }
        
        [Test]
        public void TestMongoCompletedExtractJobDoc_Constructor_ExtractJobStatus()
        {
            var doc = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider);

            Assert.AreEqual(ExtractJobStatus.Completed, doc.JobStatus);
        }

        [Test]
        public void TestMongoCompletedExtractJobDoc_Equality()
        {
            var doc1 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider);
            var doc2 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider);
            Assert.AreEqual(doc1, doc2);
        }

        [Test]
        public void TestMongoCompletedExtractJobDoc_GetHashCode()
        {
            var doc1 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider);
            var doc2 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider);
            Assert.AreEqual(doc1.GetHashCode(), doc2.GetHashCode());
        }

        #endregion
    }
}
