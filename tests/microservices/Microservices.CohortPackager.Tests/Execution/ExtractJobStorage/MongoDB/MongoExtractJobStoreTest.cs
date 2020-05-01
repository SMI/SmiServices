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
    public class MongoExtractJobStoreTest
    {
        private const string ExtractionDatabaseName = "testExtraction";

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

        private sealed class TestMongoClient : StubMongoClient
        {
            public readonly TestExtractionDatabase ExtractionDatabase = new TestExtractionDatabase();
            public readonly Mock<IClientSessionHandle> MockSessionHandle = new Mock<IClientSessionHandle>();

            public override IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
            {
                if (name == ExtractionDatabaseName)
                    return ExtractionDatabase;
                throw new ArgumentException(nameof(name));
            }

            // NOTE(rkm 2020-03-10) We don't actually implement transaction rollback as we only need to be able to start with a fresh collection for each test
            public override IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => MockSessionHandle.Object;
        }

        /// <summary>
        /// Test mock of the extraction database
        /// </summary>
        private sealed class TestExtractionDatabase : StubMongoDatabase
        {
            public readonly MockExtractCollection<Guid, MongoExtractJobDoc> InProgressCollection = new MockExtractCollection<Guid, MongoExtractJobDoc>();
            public readonly MockExtractCollection<Guid, MongoCompletedExtractJobDoc> CompletedJobCollection = new MockExtractCollection<Guid, MongoCompletedExtractJobDoc>();
            public readonly Dictionary<string, MockExtractCollection<Guid, MongoExpectedFilesDoc>> ExpectedFilesCollections = new Dictionary<string, MockExtractCollection<Guid, MongoExpectedFilesDoc>>();
            public readonly Dictionary<string, MockExtractCollection<Guid, MongoFileStatusDoc>> StatusCollections = new Dictionary<string, MockExtractCollection<Guid, MongoFileStatusDoc>>();

            public override IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings = null)
            {
                dynamic retCollection = null;
                if (name == "inProgressJobs")
                    retCollection = InProgressCollection;
                else if (name == "completedJobs")
                    retCollection = CompletedJobCollection;
                else if (name.StartsWith("expectedFiles"))
                {
                    if (!ExpectedFilesCollections.ContainsKey(name))
                        ExpectedFilesCollections[name] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
                    retCollection = ExpectedFilesCollections[name];
                }
                else if (name.StartsWith("statuses"))
                {
                    if (!StatusCollections.ContainsKey(name))
                        StatusCollections[name] = new MockExtractCollection<Guid, MongoFileStatusDoc>();
                    retCollection = StatusCollections[name];
                }

                return retCollection != null
                    ? (IMongoCollection<TDocument>)retCollection
                    : throw new ArgumentException($"No implementation for {typeof(TDocument)} with name {name}");
            }

            public override void DropCollection(string name, CancellationToken cancellationToken = new CancellationToken())
            {
                ExpectedFilesCollections.Remove(name);
                StatusCollections.Remove(name);
            }
        }

        /// <summary>
        /// Mock of a collection in the extraction database. Can be keyed by string or Guid.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal"></typeparam>
        private sealed class MockExtractCollection<TKey, TVal> : StubMongoCollection<TKey, TVal>
        {
            public readonly Dictionary<TKey, TVal> Documents = new Dictionary<TKey, TVal>();

            public bool RejectChanges { get; set; }

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
                if (RejectChanges)
                    throw new Exception("Rejecting changes");

                BsonDocument bsonDoc = document.ToBsonDocument();
                if (!bsonDoc.Contains("_id"))
                    bsonDoc.Add("_id", Guid.NewGuid().ToString());
                if (!Documents.TryAdd(GetKey(bsonDoc["_id"].ToString()), document))
                    throw new Exception("Document already exists");
            }

            public override void InsertMany(IEnumerable<TVal> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken())
            {
                foreach (TVal doc in documents)
                    InsertOne(doc, null, cancellationToken);
            }

            public override ReplaceOneResult ReplaceOne(FilterDefinition<TVal> filter, TVal replacement,
                ReplaceOptions options = null, CancellationToken cancellationToken = new CancellationToken())
            {
                if (RejectChanges)
                    return ReplaceOneResult.Unacknowledged.Instance;

                BsonDocument bsonDoc = replacement.ToBsonDocument();
                TKey key = GetKey(bsonDoc["_id"].ToString());
                if (!Documents.ContainsKey(key))
                    return ReplaceOneResult.Unacknowledged.Instance;

                Documents[key] = replacement;
                return new ReplaceOneResult.Acknowledged(1, 1, 0);
            }

            public override DeleteResult DeleteOne(FilterDefinition<TVal> filter, CancellationToken cancellationToken = new CancellationToken())
            {
                if (RejectChanges)
                    return DeleteResult.Unacknowledged.Instance;

                BsonDocument filterDoc = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<TVal>(), BsonSerializer.SerializerRegistry);
                if (!filterDoc.Contains("_id") || filterDoc.Count() > 1)
                    throw new NotImplementedException("No support for deleting multiple docs");

                return Documents.Remove(GetKey(filterDoc["_id"].ToString()))
                    ? (DeleteResult)new DeleteResult.Acknowledged(1)
                    : DeleteResult.Unacknowledged.Instance;
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
        public void TestPersistMessageToStoreImpl_ExtractionRequestInfoMessage()
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
        public void TestPersistMessageToStoreImpl_ExtractFileCollectionInfoMessage()
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

            Dictionary<Guid, MongoExpectedFilesDoc> docs = client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoExpectedFilesDoc extractJob = docs.Values.ToList()[0];

            var expected = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
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

        [Test]
        public void TestPersistMessageToStoreImpl_ExtractFileCollectionInfoMessage_NoIdentifiers()
        {
            Guid jobId = Guid.NewGuid();
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = jobId,
                ProjectNumber = "1234-5678",
                RejectionReasons = new Dictionary<string, int>
                {
                    {"ImageType is not ORIGINAL", 1 },
                },
                JobSubmittedAt = DateTime.UtcNow,
                ExtractionDirectory = "1234-5678/testExtract",
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>(), // No files were extractable for this key
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

            Assert.DoesNotThrow(() => store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header));
        }

        [Test]
        public void TestPersistMessageToStoreImpl_ExtractFileStatusMessage()
        {
            var client = new TestMongoClient();
            var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

            Guid jobId = Guid.NewGuid();
            var testExtractFileStatusMessage = new ExtractFileStatusMessage
            {
                AnonymisedFileName = "anon.dcm",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                Status = ExtractFileStatus.ErrorWontRetry,
                ProjectNumber = "1234",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = "1234/test",
                StatusMessage = "Could not anonymise",
                DicomFilePath = "original.dcm",
            };
            var header = new MessageHeader();

            store.PersistMessageToStore(testExtractFileStatusMessage, header);


            Dictionary<Guid, MongoFileStatusDoc> docs = client.ExtractionDatabase.StatusCollections[$"statuses_{jobId}"].Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoFileStatusDoc statusDoc = docs.Values.ToList()[0];

            var expected = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
                "anon.dcm",
                false,
                true,
                "Could not anonymise");

            Assert.True(statusDoc.Equals(expected));
        }

        [Test]
        public void TestPersistMessageToStoreImpl_IsIdentifiableMessage()
        {
            var client = new TestMongoClient();
            var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

            Guid jobId = Guid.NewGuid();
            var testIsIdentifiableMessage = new IsIdentifiableMessage
            {
                AnonymisedFileName = "anon.dcm",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "1234",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = "1234/test",
                DicomFilePath = "original.dcm",
                IsIdentifiable = false,
                Report = "[]", // NOTE(rkm 2020-03-10) An "empty" report from IsIdentifiable
            };
            var header = new MessageHeader();

            store.PersistMessageToStore(testIsIdentifiableMessage, header);

            Dictionary<Guid, MongoFileStatusDoc> docs = client.ExtractionDatabase.StatusCollections[$"statuses_{jobId}"].Documents;
            Assert.AreEqual(docs.Count, 1);
            MongoFileStatusDoc statusDoc = docs.Values.ToList()[0];

            var expected = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
                "anon.dcm",
                true,
                false,
                "[]");

            Assert.True(statusDoc.Equals(expected));
        }

        [Test]
        public void TestGetReadJobsImpl()
        {
            Guid jobId = Guid.NewGuid();
            var testJob = new MongoExtractJobDoc(
                jobId,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                "1234",
                ExtractJobStatus.Failed,
                "test/dir",
                _dateTimeProvider.UtcNow(),
                "SeriesInstanceUID",
                1,
                "MR",
                null);
            var testMongoExpectedFilesDoc = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                "1.2.3.4",
                new HashSet<MongoExpectedFileInfoDoc>
                {
                    new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon1.dcm"),
                },
                new MongoRejectedKeyInfoDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                    new Dictionary<string, int>())
            );
            var testMongoFileStatusDoc = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                "anon1.dcm",
                true,
                false,
                "Verified");

            var client = new TestMongoClient();
            var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

            // Assert that jobs marked as failed are not returned
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.InProgressCollection.RejectChanges = true;
            Assert.AreEqual(0, store.GetReadyJobs().Count);

            // Assert that an in progress job is not returned
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.InProgressCollection.RejectChanges = true;
            Assert.AreEqual(0, store.GetReadyJobs().Count);

            // Check we handle a bad ReplaceOneResult
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.InProgressCollection.RejectChanges = true;
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
            Assert.Throws<ApplicationException>(() => store.GetReadyJobs());

            // Check happy path
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
            Assert.AreEqual(0, store.GetReadyJobs().Count);
            Assert.AreEqual(ExtractJobStatus.WaitingForStatuses, client.ExtractionDatabase.InProgressCollection.Documents.Single().Value.JobStatus);
            client.ExtractionDatabase.StatusCollections[$"statuses_{jobId}"] = new MockExtractCollection<Guid, MongoFileStatusDoc>();
            client.ExtractionDatabase.StatusCollections[$"statuses_{jobId}"].InsertOne(testMongoFileStatusDoc);
            ExtractJobInfo job = store.GetReadyJobs().Single();
            Assert.AreEqual(ExtractJobStatus.ReadyForChecks, job.JobStatus);
            Assert.AreEqual(ExtractJobStatus.ReadyForChecks, client.ExtractionDatabase.InProgressCollection.Documents.Single().Value.JobStatus);
        }

        [Test]
        public void TestCompleteJobImpl()
        {
            Guid jobId = Guid.NewGuid();
            var testJob = new MongoExtractJobDoc(
                jobId,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                "1234",
                ExtractJobStatus.Failed,
                "test/dir",
                _dateTimeProvider.UtcNow(),
                "SeriesInstanceUID",
                1,
                "MR",
                null);
            var testMongoExpectedFilesDoc = new MongoExpectedFilesDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                "1.2.3.4",
                new HashSet<MongoExpectedFileInfoDoc>
                {
                    new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon1.dcm"),
                },
                new MongoRejectedKeyInfoDoc(
                    MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                    new Dictionary<string, int>())
                );
            var testMongoFileStatusDoc = new MongoFileStatusDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                "anon1.dcm",
                true,
                false,
                "Verified");

            var client = new TestMongoClient();
            var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

            // Assert that an exception is thrown for a non-existent job
            Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(Guid.NewGuid()));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Assert that an exception is thrown for a job which is marked as failed
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.MockSessionHandle.Reset();
            Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(Guid.NewGuid()));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Check that we handle a failed insertion
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            testJob.JobStatus = ExtractJobStatus.Completed;
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.CompletedJobCollection.RejectChanges = true;
            client.MockSessionHandle.Reset();
            Assert.Throws<Exception>(() => store.MarkJobCompleted(jobId));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Check we handle a bad DeleteResult
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            client.ExtractionDatabase.InProgressCollection.RejectChanges = true;
            client.MockSessionHandle.Reset();
            Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(jobId));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Check we handle missing expectedFiles collection
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.MockSessionHandle.Reset();
            Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(jobId));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Check we handle missing statuses collection
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
            client.MockSessionHandle.Reset();
            Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(jobId));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Check happy path
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
            client.ExtractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
            client.ExtractionDatabase.StatusCollections[$"statuses_{jobId}"] = new MockExtractCollection<Guid, MongoFileStatusDoc>();
            client.ExtractionDatabase.StatusCollections[$"statuses_{jobId}"].InsertOne(testMongoFileStatusDoc);
            client.MockSessionHandle.Reset();
            store.MarkJobCompleted(jobId);
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Never);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreEqual(1, client.ExtractionDatabase.ExpectedFilesCollections.Count);
            Assert.AreEqual(1, client.ExtractionDatabase.StatusCollections.Count);
        }

        [Test]
        public void TestMarkJobFailedImpl()
        {
            Guid jobId = Guid.NewGuid();
            var testJob = new MongoExtractJobDoc(
                jobId,
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                "1234",
                ExtractJobStatus.Failed,
                "test/dir",
                _dateTimeProvider.UtcNow(),
                "1.2.3.4",
                123,
                "MR",
                null);

            var client = new TestMongoClient();
            var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

            // Assert that an exception is thrown for a non-existent job
            Assert.Throws<ApplicationException>(() => store.MarkJobFailed(Guid.NewGuid(), new Exception()));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Assert that a job can't be failed twice
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.MockSessionHandle.Reset();
            Assert.Throws<ApplicationException>(() => store.MarkJobFailed(jobId, new Exception()));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Check we handle a bad ReplaceOneResult
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.ExtractionDatabase.InProgressCollection.RejectChanges = true;
            client.MockSessionHandle.Reset();
            Assert.Throws<ApplicationException>(() => store.MarkJobFailed(jobId, new Exception()));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

            // Check happy path
            client = new TestMongoClient();
            store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
            testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
            testJob.FailedJobInfoDoc = null;
            client.ExtractionDatabase.InProgressCollection.InsertOne(testJob);
            client.MockSessionHandle.Reset();
            store.MarkJobFailed(jobId, new Exception("TestMarkJobFailedImpl"));
            client.MockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Never);
            client.MockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
            Dictionary<Guid, MongoExtractJobDoc> docs = client.ExtractionDatabase.InProgressCollection.Documents;
            Assert.AreEqual(1, docs.Count);
            MongoExtractJobDoc failedDoc = docs[jobId];
            Assert.AreEqual(ExtractJobStatus.Failed, failedDoc.JobStatus);
            Assert.NotNull(failedDoc.FailedJobInfoDoc);
            Assert.AreEqual("TestMarkJobFailedImpl", failedDoc.FailedJobInfoDoc.ExceptionMessage);
        }

        #endregion
    }
}
