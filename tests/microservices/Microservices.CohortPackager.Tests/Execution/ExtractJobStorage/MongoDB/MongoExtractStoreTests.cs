using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
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


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB
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

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        /// <summary>
        /// Test mock of the extraction database
        /// </summary>
        private class MockExtractionDatabase : StubMongoDatabase
        {
            public readonly MockExtractCollection<Guid, MongoExtractJob> ExtractJobCollection = new MockExtractCollection<Guid, MongoExtractJob>();
            public readonly MockExtractCollection<Guid, ArchivedMongoExtractJob> ExtractJobArchiveCollection = new MockExtractCollection<Guid, ArchivedMongoExtractJob>();
            public readonly MockExtractCollection<Guid, QuarantinedMongoExtractJob> ExtractJobQuarantineCollection = new MockExtractCollection<Guid, QuarantinedMongoExtractJob>();
            public readonly MockExtractCollection<string, MongoExtractedFileStatusDocument> ExtractStatusCollection = new MockExtractCollection<string, MongoExtractedFileStatusDocument>();

            public override IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings = null)
            {
                dynamic retCollection = null;
                if (typeof(TDocument) == typeof(MongoExtractJob) && name == "extractJobStore")
                    retCollection = ExtractJobCollection;
                if (typeof(TDocument) == typeof(ArchivedMongoExtractJob) && name == "extractJobArchive")
                    retCollection = ExtractJobArchiveCollection;
                if (typeof(TDocument) == typeof(QuarantinedMongoExtractJob) && name == "extractJobQuarantine")
                    retCollection = ExtractJobQuarantineCollection;
                if (typeof(TDocument) == typeof(MongoExtractedFileStatusDocument) && name.StartsWith("statuses_"))
                    // NOTE(rkm 2020-02-25) Doesn't support multiple extractions per test for now
                    retCollection = ExtractStatusCollection;

                return retCollection != null
                    ? (IMongoCollection<TDocument>)retCollection
                    : throw new ArgumentException($"No implementation for {typeof(TDocument)} with name {name}");
            }
        }

        /// <summary>
        /// Test mock of a collection in the extraction database. Can be keyed by string or Guid.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal"></typeparam>
        private class MockExtractCollection<TKey, TVal> : StubMongoCollection<TKey, TVal>
        {
            public readonly Dictionary<TKey, TVal> Documents = new Dictionary<TKey, TVal>();

            public override long CountDocuments(FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => Documents.Count;

            public override IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TVal> filter, FindOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken())
            {
                var mockCursor = new Mock<IAsyncCursor<TProjection>>();
                mockCursor
                    .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                    .Returns(true)
                    .Returns(false);

                if (filter == FilterDefinition<TVal>.Empty)
                {
                    mockCursor
                        .Setup(x => x.Current)
                        .Returns((IEnumerable<TProjection>)Documents.Values.ToList());
                    return mockCursor.Object;
                }

                BsonDocument rendered = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<TVal>(), BsonSerializer.SerializerRegistry);

                TKey key = GetKey(rendered["_id"]);

                if (Documents.ContainsKey(key))
                {
                    mockCursor
                        .Setup(x => x.Current)
                        .Returns((IEnumerable<TProjection>)new List<TVal> { Documents[key] });
                    return mockCursor.Object;
                }

                mockCursor.Reset();
                mockCursor
                    .Setup(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                    .Returns(false);
                return mockCursor.Object;
            }

            public override void InsertOne(TVal document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken())
            {
                BsonDocument bsonDoc = document.ToBsonDocument();
                if (!bsonDoc.Contains("_id"))
                    bsonDoc.Add("_id", new BsonObjectId(ObjectId.GenerateNewId()));

                if (!Documents.TryAdd(GetKey(bsonDoc["_id"]), document))
                    throw new Exception("Document already exists");
            }

            private static TKey GetKey(dynamic key)
            {
                if (typeof(TKey) == typeof(string))
                    return key;
                if (typeof(TKey) == typeof(Guid))
                    // Dynamic typing is fun!
                    return (TKey)Convert.ChangeType(Guid.Parse(((BsonString)key).Value), typeof(TKey));

                throw new Exception($"Unsupported key type {typeof(TKey)}");
            }
        }

        #endregion

        #region Tests 

        [Test]
        public void TestPersistJobInfoToStore_ExtractionRequestInfoMessage()
        {
            var db = new MockExtractionDatabase();
            var timeProvider = new TestDateTimeProvider();
            var store = new MongoExtractJobStore(db, timeProvider);
            Guid jobId = Guid.NewGuid();
            DateTime jobSubmittedAt = DateTime.UtcNow;
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtract",
                JobSubmittedAt = jobSubmittedAt,
                KeyTag = "StudyInstanceUID",
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

            var expected = new MongoExtractJob
            {
                ExtractionModality = "CT",
                JobSubmittedAt = jobSubmittedAt,
                ProjectNumber = "1234-5678",
                ExtractionJobIdentifier = jobId,
                KeyTag = "StudyInstanceUID",
                ExtractionDirectory = "1234-5678/testExtract",
                Header = MongoExtractJobHeader.FromMessageHeader(testHeader, timeProvider),
                JobStatus = ExtractJobStatus.WaitingForCollectionInfo,
                FileCollectionInfo = new List<MongoExpectedFilesForKey>(),
                KeyCount = 1,
            };

            Assert.AreEqual(expected, extractJob);
        }

        [Test]
        public void TestPersistJobInfoToStore_ExtractFileCollectionInfoMessage()
        {
            var db = new MockExtractionDatabase();
            var timeProvider = new TestDateTimeProvider();
            var store = new MongoExtractJobStore(db, timeProvider);
            Guid jobId = Guid.NewGuid();
            DateTime jobSubmittedAt = DateTime.UtcNow;
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                RejectionReasons = new Dictionary<string, int>(),
                JobSubmittedAt = jobSubmittedAt,
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
            Assert.AreEqual(testExtractFileCollectionInfoMessage.ExtractionJobIdentifier, extractJob.ExtractionJobIdentifier);
            Assert.AreEqual(testExtractFileCollectionInfoMessage.JobSubmittedAt, extractJob.JobSubmittedAt);
            Assert.AreEqual(new List<MongoExpectedFilesForKey>(), extractJob.FileCollectionInfo);
            Assert.AreEqual(ExtractJobStatus.WaitingForStatuses, extractJob.JobStatus);
            Assert.AreEqual(testHeader.MessageGuid, extractJob.Header.ExtractRequestInfoMessageGuid);
            Assert.AreEqual($"{testHeader.ProducerExecutableName}({testHeader.ProducerProcessID})", extractJob.Header.ProducerIdentifier);
            Assert.True((DateTime.UtcNow - extractJob.Header.ReceivedAt).TotalSeconds < 1);
        }

        [Test]
        public void TestPersistJobInfoToStore_ExtractFileStatusMessage()
        {

            var db = new MockExtractionDatabase();
            var store = new MongoExtractJobStore(db);
            var testExtractFileCollectionInfoMessage = new ExtractFileStatusMessage
            {
                ExtractionJobIdentifier = Guid.NewGuid(),
                ProjectNumber = "1234-5678",
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionDirectory = "1234-5678/testExtract",
                Status = ExtractFileStatus.ErrorWontRetry,
                AnonymisedFileName = "test-dicom-an.dcm",
                DicomFilePath = "test-dicom.dcm",
                StatusMessage = "Could not anonymise - blah",
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

            //Dictionary<Guid, MongoExtractJob> docs = db.ExtractJobCollection.Documents;
            //Assert.AreEqual(docs.Count, 1);
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
