using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB.ObjectModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.MongoDb.Tests;
using Smi.Common.MongoDB.Tests;
using Smi.Common.Tests;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage.MongoDB
{

    [TestFixture]
    public class MongoExtractStoreTests
    {
        private const string ExtractionDatabaseName = "extraction";

        private readonly TestDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

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

        private class TestMongoClient : StubMongoClient
        {
            public readonly TestExtractionDatabase ExtractionDatabase = new TestExtractionDatabase();

            public override IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
            {
                if (name == ExtractionDatabaseName)
                    return ExtractionDatabase;
                throw new ArgumentException(nameof(name));
            }
        }

        /// <summary>
        /// Test mock of the extraction database
        /// </summary>
        private class TestExtractionDatabase : StubMongoDatabase
        {
            public readonly MockExtractCollection<Guid, MongoExtractJobDoc> InProgressCollection = new MockExtractCollection<Guid, MongoExtractJobDoc>();
            public readonly MockExtractCollection<Guid, MongoCompletedExtractJobDoc> CompletedCollection = new MockExtractCollection<Guid, MongoCompletedExtractJobDoc>();

            public readonly MockExtractCollection<Guid, MongoExpectedFilesDoc> ExpectedFilesCollection = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
            public readonly MockExtractCollection<Guid, MongoExpectedFilesDoc> CompletedExpectedFilesCollection = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();

            public readonly MockExtractCollection<string, MongoFileStatusDoc> ExtractStatusCollection = new MockExtractCollection<string, MongoFileStatusDoc>();
            public readonly MockExtractCollection<string, MongoFileStatusDoc> CompletedExtractStatusCollection = new MockExtractCollection<string, MongoFileStatusDoc>();

            public override IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings = null)
            {
                // NOTE(rkm 2020-02-25) Doesn't support multiple extractions per test for now
                dynamic retCollection = null;
                if (typeof(TDocument) == typeof(MongoExtractJobDoc) && name == "inProgress")
                    retCollection = InProgressCollection;
                else if (typeof(TDocument) == typeof(MongoCompletedExtractJobDoc) && name == "completed")
                    retCollection = CompletedCollection;
                else if (typeof(TDocument) == typeof(MongoExpectedFilesDoc) && name.StartsWith("expected"))
                    retCollection = name.EndsWith("completed") ? CompletedExpectedFilesCollection : ExpectedFilesCollection;
                else if (typeof(TDocument) == typeof(MongoExpectedFilesDoc) && name.StartsWith("statuses"))
                    retCollection = name.EndsWith("completed") ? CompletedExtractStatusCollection : ExtractStatusCollection;

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
                    bsonDoc.Add("_id", Guid.NewGuid().ToString());
                if (!Documents.TryAdd(GetKey(bsonDoc["_id"].ToString()), document))
                    throw new Exception("Document already exists");
            }

            public override ReplaceOneResult ReplaceOne(FilterDefinition<TVal> filter, TVal replacement,
                UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken())
            {
                BsonDocument bsonDoc = replacement.ToBsonDocument();
                TKey key = GetKey(bsonDoc["_id"].ToString());
                if (!Documents.ContainsKey(key))
                    return ReplaceOneResult.Unacknowledged.Instance;

                Documents[key] = replacement;
                return new ReplaceOneResult.Acknowledged(1, 1, 0);
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
            Guid guid = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = guid,
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtract",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 1,
                ExtractionModality = "CT",
            };
            var testHeader = new MessageHeader
            {
                MessageGuid = Guid.NewGuid(),
                OriginalPublishTimestamp = MessageHeader.UnixTime(_dateTimeProvider.UtcNow()),
                Parents = new[] { Guid.NewGuid(), },
                ProducerExecutableName = "MongoExtractStoreTests",
                ProducerProcessID = 1234,
            };

            var client = new TestMongoClient();
            var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

            store.PersistMessageToStore(testExtractionRequestInfoMessage, testHeader);

            Dictionary<Guid, MongoExtractJobDoc> docs = client.ExtractionDatabase.InProgressCollection.Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExtractJobDoc extractJob = docs.Values.ToList()[0];

            var expected = new MongoExtractJobDoc(
                guid,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(guid, testHeader, _dateTimeProvider),
                "1234-5678",
                ExtractJobStatus.WaitingForCollectionInfo,
                "1234-5678/testExtract",
                _dateTimeProvider.UtcNow(),
                "StudyInstanceUID",
                1,
                "CT",
                null);

            Assert.AreEqual(expected, extractJob);
        }

        [Test]
        public void TestPersistJobInfoToStore_ExtractFileCollectionInfoMessage()
        {
            Guid jobId = Guid.NewGuid();
            var header1 = new MessageHeader();
            var header2 = new MessageHeader();
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                RejectionReasons = new Dictionary<string, int>
                {
                    {"reject1", 1 },
                    {"reject2", 2 },
                },
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionDirectory = "1234-5678/testExtract",
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { header1, "file1" },
                    { header2, "file2" }
                },
                KeyValue = "series-1",
            };
            var header = new MessageHeader
            {
                MessageGuid = Guid.NewGuid(),
                OriginalPublishTimestamp = MessageHeader.UnixTimeNow(),
                Parents = new[] { Guid.NewGuid(), },
                ProducerExecutableName = "MongoExtractStoreTests",
                ProducerProcessID = 1234,
            };

            var client = new TestMongoClient();
            var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

            store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header);

            Dictionary<Guid, MongoExpectedFilesDoc> docs = client.ExtractionDatabase.ExpectedFilesCollection.Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExpectedFilesDoc extractJob = docs.Values.ToList()[0];

            var expected = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header1, _dateTimeProvider),
                "series-1",
                new HashSet<MongoExpectedFileInfoDoc>
                {
                    new MongoExpectedFileInfoDoc(header1.MessageGuid, "file1"),
                    new MongoExpectedFileInfoDoc(header2.MessageGuid, "file2"),
                },
                new MongoRejectedKeyInfoDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
                    new Dictionary<string, int>
                    {
                        {"reject1", 1 },
                        {"reject2", 2 },
                    })
                );

            Assert.True(extractJob.Equals(expected));
        }
#if false

        [Test]
        public void TestPersistJobInfoToStore_ExtractFileStatusMessage()
        {
            var db = new MockExtractionDatabase();
            var dateTimeProvider = new TestDateTimeProvider();
            var store = new MongoExtractJobStore(db, dateTimeProvider);
            Guid jobId = Guid.NewGuid();
            var testExtractFileCollectionInfoMessage = new ExtractFileStatusMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionDirectory = "1234-5678/testExtract",
                Status = ExtractFileStatus.ErrorWontRetry,
                AnonymisedFileName = "test-dicom-an.dcm",
                DicomFilePath = "test-dicom.dcm",
                StatusMessage = "Could not anonymise - blah",
            };
            Guid headerGuid = Guid.NewGuid();
            var testHeader = new MessageHeader
            {
                MessageGuid = headerGuid,
                OriginalPublishTimestamp = MessageHeader.UnixTimeNow(),
                Parents = new Guid[0],
                ProducerExecutableName = "MongoExtractStoreTests",
                ProducerProcessID = 1234,
            };

            store.PersistMessageToStore(testExtractFileCollectionInfoMessage, testHeader);

            Dictionary<string, MongoExtractedFileStatusDocument> docs = db.ExtractStatusCollection.Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExtractedFileStatusDocument statusDoc = docs.Values.ToList()[0];

            var expected = new MongoExtractedFileStatusDocument
            {
                Status = "ErrorWontRetry",
                Header = new MongoExtractedFileStatusHeaderDocument
                {
                    ProducerIdentifier = "MongoExtractStoreTests(1234)",
                    ReceivedAt = dateTimeProvider.UtcNow(),
                    FileStatusMessageGuid = headerGuid,
                },
                AnonymisedFileName = null,
                StatusMessage = "Could not anonymise - blah",
            };

            Assert.True(statusDoc.Equals(expected));
        }

        [Test]
        public void TestPersistJobInfoToStore_IsIdentifiableMessage()
        {
            var db = new MockExtractionDatabase();
            var dateTimeProvider = new TestDateTimeProvider();
            var store = new MongoExtractJobStore(db, dateTimeProvider);
            Guid jobId = Guid.NewGuid();
            var testIsIdentifiableMessage = new IsIdentifiableMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionDirectory = "1234-5678/testExtract",
                AnonymisedFileName = "test-dicom-an.dcm",
                DicomFilePath = "test-dicom.dcm",
                Report = "Report text",
                IsIdentifiable = false,
            };
            Guid headerGuid = Guid.NewGuid();
            var testHeader = new MessageHeader
            {
                MessageGuid = headerGuid,
                OriginalPublishTimestamp = MessageHeader.UnixTimeNow(),
                Parents = new Guid[0],
                ProducerExecutableName = "MongoExtractStoreTests",
                ProducerProcessID = 1234,
            };

            store.PersistMessageToStore(testIsIdentifiableMessage, testHeader);

            Dictionary<string, MongoExtractedFileStatusDocument> docs = db.ExtractStatusCollection.Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExtractedFileStatusDocument statusDoc = docs.Values.ToList()[0];

            var expected = new MongoExtractedFileStatusDocument
            {
                Status = "Verified",
                Header = new MongoExtractedFileStatusHeaderDocument
                {
                    ProducerIdentifier = "MongoExtractStoreTests(1234)",
                    ReceivedAt = dateTimeProvider.UtcNow(),
                    FileStatusMessageGuid = headerGuid,
                },
                AnonymisedFileName = "test-dicom-an.dcm",
                StatusMessage = "Report text",
            };

            Assert.True(statusDoc.Equals(expected));
        }

        [Test]
        public void TestPersistJobInfoToStore_All()
        {
            var db = new MockExtractionDatabase();
            var dateTimeProvider = new TestDateTimeProvider();
            var store = new MongoExtractJobStore(db, dateTimeProvider);

            Guid jobId = Guid.NewGuid();
            DateTime jobSubmittedAt = DateTime.UtcNow;

            var header = new MessageHeader();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtract",
                JobSubmittedAt = jobSubmittedAt,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 2,
                ExtractionModality = "CT",
            };
            var infoHeader1 = new MessageHeader();
            var infoHeader2 = new MessageHeader();
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                RejectionReasons = new Dictionary<string, int>
                {
                    {"Not extractable - blah", 5 },
                },
                JobSubmittedAt = jobSubmittedAt,
                ExtractionDirectory = "1234-5678/testExtract",
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { infoHeader1, "file1" },
                    { infoHeader2, "file2" }
                },
                KeyValue = "series-1",
            };

            store.PersistMessageToStore(testExtractionRequestInfoMessage, header);
            store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header);
            testExtractFileCollectionInfoMessage.KeyValue = "series-2";
            store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header);

            Dictionary<Guid, MongoExtractJob> docs = db.ExtractJobCollection.Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExtractJob extractJob = docs.Values.ToList()[0];

            var expected = new MongoExtractJob
            {
                ExtractionModality = "CT",
                JobSubmittedAt = jobSubmittedAt,
                ProjectNumber = "1234-5678",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = "1234-5678/testExtract",
                KeyTag = "StudyInstanceUID",
                JobStatus = ExtractJobStatus.WaitingForStatuses,
                Header = MongoExtractJobHeader.FromMessageHeader(header, dateTimeProvider),
                ExpectedFilesInfo = new List<MongoExpectedFilesForKey>
                {
                    new MongoExpectedFilesForKey
                    {
                        Header =  ExtractFileCollectionHeader.FromMessageHeader(header, dateTimeProvider),
                        AnonymisedFiles =  new List<ExpectedAnonymisedFileInfo>
                        {
                            new ExpectedAnonymisedFileInfo { ExtractFileMessageGuid = infoHeader1.MessageGuid, AnonymisedFilePath = "file1" },
                            new ExpectedAnonymisedFileInfo { ExtractFileMessageGuid = infoHeader2.MessageGuid, AnonymisedFilePath = "file2" },
                        },
                        Key = "series-1",
                    },
                    new MongoExpectedFilesForKey
                    {
                        Header =  ExtractFileCollectionHeader.FromMessageHeader(header, dateTimeProvider),
                        AnonymisedFiles =  new List<ExpectedAnonymisedFileInfo>
                        {
                            new ExpectedAnonymisedFileInfo { ExtractFileMessageGuid = infoHeader1.MessageGuid, AnonymisedFilePath = "file1" },
                            new ExpectedAnonymisedFileInfo { ExtractFileMessageGuid = infoHeader2.MessageGuid, AnonymisedFilePath = "file2" },
                        },
                        Key = "series-2",
                    },
                },
                RejectedKeysInfo = new List<MongoRejectedKeyInfoDoc>
                {
                    new MongoRejectedKeyInfoDoc
                    {
                        Header = ExtractFileCollectionHeader.FromMessageHeader(header, dateTimeProvider),
                        Key = "series-1",
                        RejectionInfo = new Dictionary<string, int>
                        {
                            { "Not extractable - blah", 5 }
                        }
                    },
                    new MongoRejectedKeyInfoDoc
                    {
                        Header = ExtractFileCollectionHeader.FromMessageHeader(header, dateTimeProvider),
                        Key = "series-2",
                        RejectionInfo = new Dictionary<string, int>
                        {
                            { "Not extractable - blah", 5 }
                        }
                    },
                },
                KeyCount = 2,
                ReceivedCollectionInfoMessages = 2,
            };

            Assert.True(extractJob.Equals(expected));
        }

        [Test]
        public void TestGetLatestJobInfo()
        {

            var db = new MockExtractionDatabase();
            var dateTimeProvider = new TestDateTimeProvider();
            var store = new MongoExtractJobStore(db, dateTimeProvider);

            Guid jobId = Guid.NewGuid();
            DateTime jobSubmittedAt = DateTime.UtcNow;

            var header = new MessageHeader();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                ExtractionDirectory = "1234-5678/testExtract",
                JobSubmittedAt = jobSubmittedAt,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 2,
                ExtractionModality = "CT",
            };
            var infoHeader1 = new MessageHeader();
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                RejectionReasons = new Dictionary<string, int>
                {
                    {"Not extractable - blah", 5 },
                },
                JobSubmittedAt = jobSubmittedAt,
                ExtractionDirectory = "1234-5678/testExtract",
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { infoHeader1, "file1" },
                },
                KeyValue = "series-1",
            };

            store.PersistMessageToStore(testExtractionRequestInfoMessage, header);
            store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header);
            store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header);

            List<ExtractJobInfo> jobInfo = store.GetLatestJobInfo();
            Assert.AreEqual(1, jobInfo.Count);
        }

        [Test]
        public void TestCleanupJobData()
        {
            Assert.Fail();
        }

        [Test]
        public void TestQuarantine()
        {
            // Merge test(?)
            Assert.Fail();
        }
#endif
        #endregion
    }
}
