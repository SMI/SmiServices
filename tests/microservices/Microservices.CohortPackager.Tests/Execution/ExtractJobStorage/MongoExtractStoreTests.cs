using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDocuments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.MongoDB.Tests;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    [TestFixture]
    public class MongoExtractStoreTests
    {
        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        #endregion

        #region Test Methods 

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        /// <summary>
        /// Test mock of the extraction database
        /// </summary>
        private class TestExtractionDatabase : TestMongoDatabase
        {
            public readonly TestExtractCollection<MongoExtractJob> ExtractJobCollection = new TestExtractCollection<MongoExtractJob>();
            public readonly TestExtractCollection<ArchivedMongoExtractJob> ExtractJobArchiveCollection = new TestExtractCollection<ArchivedMongoExtractJob>();
            public readonly TestExtractCollection<QuarantinedMongoExtractJob> ExtractJobQuarantineCollection = new TestExtractCollection<QuarantinedMongoExtractJob>();

            public override IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings = null)
            {
                if (typeof(TDocument) == typeof(MongoExtractJob) && name == "extractJobStore")
                    return (IMongoCollection<TDocument>)ExtractJobCollection;
                if (typeof(TDocument) == typeof(ArchivedMongoExtractJob) && name == "extractJobArchive")
                    return (IMongoCollection<TDocument>)ExtractJobArchiveCollection;
                if (typeof(TDocument) == typeof(QuarantinedMongoExtractJob) && name == "extractJobQuarantine")
                    return (IMongoCollection<TDocument>)ExtractJobQuarantineCollection;
                throw new ArgumentException($"No implementation for {typeof(TDocument)} with name {name}");
            }
        }

        /// <summary>
        /// Test mock of the extraction collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class TestExtractCollection<T> : TestMongoCollection<T>
        {
            public readonly Dictionary<Guid, T> Documents = new Dictionary<Guid, T>();

            public override long CountDocuments(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => Documents.Count;

            public override IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
            {
                var mockCursor = new Mock<IAsyncCursor<TProjection>>();
                mockCursor
                    .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                    .Returns(true)
                    .Returns(false);

                if (filter == FilterDefinition<T>.Empty)
                {
                    mockCursor
                        .Setup(x => x.Current)
                        .Returns((IEnumerable<TProjection>)Documents.Values.ToList());
                    return mockCursor.Object;
                }

                BsonDocument rendered = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry);
                Guid key = Guid.Parse(rendered["_id"].AsString);
                if (Documents.ContainsKey(key))
                {
                    mockCursor
                        .Setup(x => x.Current)
                        .Returns((IEnumerable<TProjection>)new List<T> { Documents[key] });
                    return mockCursor.Object;
                }

                mockCursor.Reset();
                mockCursor
                    .Setup(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                    .Returns(false);
                return mockCursor.Object;
            }

            public override void InsertOne(T document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken())
            {
                if (!Documents.TryAdd(Guid.Parse(document.ToBsonDocument()["_id"].AsString), document))
                    throw new Exception("Document already exists");
            }
        }

        #endregion

        #region Tests 

        [Test]
        public void TestPersistJobInfoToStore_ExtractionRequestInfoMessage()
        {
            var db = new TestExtractionDatabase();
            var store = new MongoExtractJobStore(db);
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtract",
                JobSubmittedAt = DateTime.UtcNow,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 1,
                ExtractionModality = "CT",
            };
            var testHeader = new MessageHeader
            {
                MessageGuid = Guid.NewGuid(),
                OriginalPublishTimestamp = MessageHeader.UnixTimeNow(),
                Parents = new Guid[0],
                ProducerExecutableName = "MongoExtractStoreTests",
                ProducerProcessID = new Random().Next(),
            };

            store.PersistMessageToStore(testExtractionRequestInfoMessage, testHeader);

            Dictionary<Guid, MongoExtractJob> docs = db.ExtractJobCollection.Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExtractJob extractJob = docs.Values.ToList()[0];
            Assert.AreEqual(testExtractionRequestInfoMessage.ExtractionJobIdentifier, extractJob.ExtractionJobIdentifier);
            Assert.AreEqual(testExtractionRequestInfoMessage.ProjectNumber, extractJob.ProjectNumber);
            Assert.AreEqual(testExtractionRequestInfoMessage.ExtractionDirectory, extractJob.ExtractionDirectory);
            Assert.AreEqual(testExtractionRequestInfoMessage.JobSubmittedAt, extractJob.JobSubmittedAt);
            Assert.AreEqual(testExtractionRequestInfoMessage.KeyTag, extractJob.KeyTag);
            Assert.AreEqual(testExtractionRequestInfoMessage.ExtractionModality, extractJob.ExtractionModality);
            Assert.AreEqual(new List<MongoExtractFileCollection>(), extractJob.FileCollectionInfo);
            Assert.AreEqual(ExtractJobStatus.WaitingForCollectionInfo, extractJob.JobStatus);
            Assert.AreEqual(testExtractionRequestInfoMessage.KeyValueCount, extractJob.KeyCount);
            Assert.AreEqual(testHeader.MessageGuid, extractJob.Header.ExtractRequestInfoMessageGuid);
            Assert.AreEqual($"{testHeader.ProducerExecutableName}({testHeader.ProducerProcessID})", extractJob.Header.ProducerIdentifier);
            Assert.True((DateTime.UtcNow - extractJob.Header.ReceivedAt).TotalSeconds < 1); // Eeehhhh...
        }

        [Test]
        public void TestPersistJobInfoToStore_ExtractFileCollectionInfoMessage()
        {
            var db = new TestExtractionDatabase();
            var store = new MongoExtractJobStore(db);
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234-5678",
                RejectionReasons = new Dictionary<string, int>(),
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionDirectory = "1234-5678/testExtract",
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>(),
                KeyValue = "",
            };
            var testHeader = new MessageHeader
            {
                MessageGuid = Guid.NewGuid(),
                OriginalPublishTimestamp = MessageHeader.UnixTimeNow(),
                Parents = new Guid[0],
                ProducerExecutableName = "MongoExtractStoreTests",
                ProducerProcessID = new Random().Next(),
            };

            store.PersistMessageToStore(testExtractFileCollectionInfoMessage, testHeader);

            Dictionary<Guid, MongoExtractJob> docs = db.ExtractJobCollection.Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExtractJob extractJob = docs.Values.ToList()[0];
            // TODO Check all tested
            Assert.AreEqual(testExtractFileCollectionInfoMessage.ExtractionJobIdentifier, extractJob.ExtractionJobIdentifier);
            Assert.AreEqual(testExtractFileCollectionInfoMessage.JobSubmittedAt, extractJob.JobSubmittedAt);
            Assert.AreEqual(new List<MongoExtractFileCollection>(), extractJob.FileCollectionInfo);
            Assert.AreEqual(ExtractJobStatus.WaitingForStatuses, extractJob.JobStatus);
            Assert.AreEqual(testHeader.MessageGuid, extractJob.Header.ExtractRequestInfoMessageGuid);
            Assert.AreEqual($"{testHeader.ProducerExecutableName}({testHeader.ProducerProcessID})", extractJob.Header.ProducerIdentifier);
            Assert.True((DateTime.UtcNow - extractJob.Header.ReceivedAt).TotalSeconds < 1);
        }

        [Test]
        public void TestPersistJobInfoToStore_ExtractFileStatusMessage()
        {


        }

        [Test]
        public void TestPersistJobInfoToStore_IsIdentifiableMessage()
        {

        }

#if false

        [Test]
        public void TestGetJobInfo_Basic()
        {
            var store = new MongoExtractJobStore(_mongoDbOptions);

            store.PersistMessageToStore(_testExtractionRequestInfoMessage, _testExtractionRequestInfoMessageHeader);

            Assert.AreEqual(store.GetLatestJobInfo().Count, 0);

            store.PersistMessageToStore(_testFileCollectionInfoMessage, _testFileCollectionInfoMessageHeader);
            store.PersistMessageToStore(_testStatusMessageOk, _testStatusMessageHeader);
            store.PersistMessageToStore(_testStatusMessageWillRetry, _testStatusMessageHeader);
            store.PersistMessageToStore(_testStatusMessageWontRetry, _testStatusMessageHeader);

            List<ExtractJobInfo> toProcess = store.GetLatestJobInfo();

            Assert.AreEqual(toProcess.Count, 1);
            Assert.AreEqual(toProcess[0], _expectedBasicJobInfo);
        }

        [Test]
        public void TestGetLatestJobInfo()
        {
            // Should be 1 document in the collection after this
            TestPersistJobInfoToStore_Basic();

            var store = new MongoExtractJobStore(_mongoDbOptions);

            List<ExtractJobInfo> jobs = store.GetLatestJobInfo();

            Assert.AreEqual(1, jobs.Count);
            Assert.AreEqual(jobs[0], _expectedBasicJobInfo);
        }

        [Test]
        public void TestCleanupJobData()
        {

            // Put one job in the collection
            TestPersistJobInfoToStore_Basic();

            var store = new MongoExtractJobStore(_mongoDbOptions);
            IMongoCollection<ArchivedMongoExtractJob> archiveCollection =
 _testJobDatabase.GetCollection<ArchivedMongoExtractJob>("extractJobArchive");

            store.CleanupJobData(_extractionIdentifier);

            long jobStoreCount = _testExtractJobStoreCollection.CountDocuments(FilterDefinition<MongoExtractJob>.Empty);
            long archiveCount = archiveCollection.CountDocuments(FilterDefinition<ArchivedMongoExtractJob>.Empty);

            Assert.AreEqual(0, jobStoreCount);
            Assert.AreEqual(1, archiveCount);

            ArchivedMongoExtractJob archivedJob =
 archiveCollection.Find(FilterDefinition<ArchivedMongoExtractJob>.Empty).Single();

            DateTime now = DateTime.UtcNow;
            Assert.True(now - archivedJob.ArchivedAt < TimeSpan.FromSeconds(5));
            Assert.True(archivedJob.JobStatus == ExtractJobStatus.Archived);
        }

        [Test]
        public void TestQuarantine()
        {
            var store = new MongoExtractJobStore(_mongoDbOptions);
            IMongoCollection<BsonDocument> quarantineCollection =
 _testJobDatabase.GetCollection<BsonDocument>("extractJobQuarantine");

            TestPersistJobInfoToStore_Basic();

            store.QuarantineJob(_testJobInfo, new Exception("Aaaahh!"));

            long jobStoreCount = _testExtractJobStoreCollection.Count(new BsonDocument());
            long quarantine = quarantineCollection.Count(new BsonDocument());

            Assert.AreEqual(0, jobStoreCount);
            Assert.AreEqual(1, quarantine);

            BsonDocument quarantined = quarantineCollection.Find(new BsonDocument()).SingleOrDefault();
            quarantined.Remove("_id");
            quarantined.Remove("jobInfoMessages");

            var expected = new BsonDocument
            {
                {"projectNumber", "1234" },
                {"jobSubmittedAt", _defaultSubmittedDateTime },
                {"keyTag", "keyTag!" },
                {"quarantineReason", "Aaaahh!" }
            };

            Assert.AreEqual(expected, quarantined);
        }

        [Test]
        public void TestQuarantineMerge()
        {
            TestQuarantine();

            var store = new MongoExtractJobStore(_mongoDbOptions);
            IMongoCollection<BsonDocument> quarantineCollection =
 _testJobDatabase.GetCollection<BsonDocument>("extractJobQuarantine");

            Guid newInfoMessageGuid = Guid.NewGuid();

            _testJobInfo.ExtractInfoItems = new List<ExtractInfoItem>
            {
                new ExtractInfoItem(newInfoMessageGuid, DateTime.Now, "C:\\Temp", "keyValue!", new [] { Guid.NewGuid() })
            };

            store.PersistJobInfoToStore(_testJobInfo);

            long jobStoreCount = _testExtractJobStoreCollection.Count(new BsonDocument());
            long quarantine = quarantineCollection.Count(new BsonDocument());

            Assert.AreEqual(0, jobStoreCount);
            Assert.AreEqual(1, quarantine);

            BsonDocument quarantined = quarantineCollection.Find(new BsonDocument()).SingleOrDefault();
            BsonDocument jobInfoDocument = quarantined["jobInfoMessages"].AsBsonDocument;

            Assert.AreEqual(2, jobInfoDocument.ElementCount);
            Assert.NotNull(jobInfoDocument.GetElement(newInfoMessageGuid.ToString()));
        }
#endif

        #endregion
    }
}
