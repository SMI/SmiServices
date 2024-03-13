using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages;
using Smi.Common.Tests;
using System;
using System.Reflection;

namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB.ObjectModel
{
    [TestFixture]
    public class MongoCompletedExtractJobDocTest
    {
        private static readonly DateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        private readonly MongoExtractJobDoc _testExtractJobDoc = new(
            Guid.NewGuid(),
            MongoExtractionMessageHeaderDoc.FromMessageHeader(Guid.NewGuid(), new MessageHeader { Parents = new[] { Guid.NewGuid() } }, _dateTimeProvider),
            "1234",
            ExtractJobStatus.ReadyForChecks,
            "test",
            DateTime.UtcNow,
            "test",
            1,
            "testUser",
            null,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true,
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
        public void Test_MongoCompletedExtractJobDoc_ParseOldFormat()
        {
            Console.WriteLine(Guid.NewGuid());
            const string jsonDoc = @"
{
    '_id' : 'bfead735-d5c0-4f7c-b0a7-88d873704dab',
    'header' : {
        'extractionJobIdentifier' : 'bfead735-d5c0-4f7c-b0a7-88d873704dab',
        'messageGuid' : 'bfead735-d5c0-4f7c-b0a7-88d873704dab',
        'producerExecutableName' : 'ExtractorCL',
        'producerProcessID' : 1234,
        'originalPublishTimestamp' : ISODate('2020-08-28T12:00:00Z'),
        'parents' : '',
        'receivedAt' : ISODate('2020-08-28T12:00:00Z')
    },
    'projectNumber' : '1234s',
    'jobStatus' : 'Completed',
    'extractionDirectory' : 'foo/bar',
    'jobSubmittedAt' : ISODate('2020-08-28T12:00:00Z'),
    'keyTag' : 'SeriesInstanceUID',
    'keyCount' : 123,
    'extractionModality' : null,
    'failedJobInfo' : null,
    'completedAt' : ISODate('2020-08-28T12:00:00Z'),
}";

            var mongoExtractJobDoc = BsonSerializer.Deserialize<MongoCompletedExtractJobDoc>(BsonDocument.Parse(jsonDoc));

            Assert.Multiple(() =>
            {
                // NOTE(rkm 2020-08-28) This works by chance since the missing bool will default to false, so we don't require MongoCompletedExtractJobDoc to implement ISupportInitialize
                Assert.That(mongoExtractJobDoc.IsIdentifiableExtraction, Is.False);
                Assert.That(mongoExtractJobDoc.IsNoFilterExtraction, Is.False);
            });
        }

        [Test]
        public void TestMongoCompletedExtractJobDoc_SettersAvailable()
        {
            foreach (PropertyInfo p in typeof(MongoCompletedExtractJobDoc).GetProperties())
                Assert.That(p.CanWrite, Is.True, $"Property '{p.Name}' is not writeable");
        }

        [Test]
        public void TestMongoCompletedExtractJobDoc_Constructor_ExtractJobStatus()
        {
            var doc = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider.UtcNow());

            Assert.That(doc.JobStatus, Is.EqualTo(ExtractJobStatus.Completed));
        }

        [Test]
        public void TestMongoCompletedExtractJobDoc_Equality()
        {
            var doc1 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider.UtcNow());
            var doc2 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider.UtcNow());
            Assert.That(doc2, Is.EqualTo(doc1));
        }

        [Test]
        public void TestMongoCompletedExtractJobDoc_GetHashCode()
        {
            var doc1 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider.UtcNow());
            var doc2 = new MongoCompletedExtractJobDoc(_testExtractJobDoc, _dateTimeProvider.UtcNow());
            Assert.That(doc2.GetHashCode(), Is.EqualTo(doc1.GetHashCode()));
        }

        #endregion
    }
}
