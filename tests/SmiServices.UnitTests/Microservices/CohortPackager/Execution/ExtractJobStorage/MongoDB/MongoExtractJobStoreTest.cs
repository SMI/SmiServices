using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.MessageSerialization;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage.MongoDB;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage.MongoDB.ObjectModel;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.Common.MongoDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace SmiServices.UnitTests.Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;

public class MongoExtractJobStoreTest
{
    private const string ExtractionDatabaseName = "testExtraction";

    private readonly TestDateTimeProvider _dateTimeProvider = new();

    #region Fixture Methods 

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    [SetUp]
    public void SetUp()
    {
        _extractionDatabase = new TestExtractionDatabase();
        _mockSessionHandle.Reset();
    }

    [TearDown]
    public void TearDown() { }

    private static TestExtractionDatabase _extractionDatabase = new();
    private static readonly Mock<IClientSessionHandle> _mockSessionHandle = new();

    private static IMongoClient GetTestMongoClient()
    {
        _extractionDatabase = new TestExtractionDatabase();

        var m = new Mock<IMongoClient>();
        m.Setup(static m => m.GetDatabase(ExtractionDatabaseName, It.IsAny<MongoDatabaseSettings>()))
            .Returns(_extractionDatabase);
        m.Setup(static m => m.StartSession(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .Returns(_mockSessionHandle.Object);
        return m.Object;
    }

    /// <summary>
    /// Test mock of the extraction database
    /// </summary>
    private sealed class TestExtractionDatabase : StubMongoDatabase
    {
        public readonly MockExtractCollection<Guid, MongoExtractJobDoc> InProgressCollection = new();
        public readonly MockExtractCollection<Guid, MongoCompletedExtractJobDoc> CompletedJobCollection = new();
        public readonly Dictionary<string, MockExtractCollection<Guid, MongoExpectedFilesDoc>> ExpectedFilesCollections = [];
        public readonly Dictionary<string, MockExtractCollection<Guid, MongoFileStatusDoc>> StatusCollections = [];

        public override IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings? settings = null)
        {
            dynamic? retCollection = null;
            switch (name)
            {
                case "inProgressJobs":
                    retCollection = InProgressCollection;
                    break;
                case "completedJobs":
                    retCollection = CompletedJobCollection;
                    break;
                default:
                    {
                        if (name.StartsWith("expectedFiles"))
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

                        break;
                    }
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
    private sealed class MockExtractCollection<TKey, TVal> : StubMongoCollection<TKey, TVal> where TKey : struct
    {
        public readonly Dictionary<TKey, TVal> Documents = [];

        public bool RejectChanges { get; set; }

        public override long CountDocuments(FilterDefinition<TVal> filter, CountOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => Documents.Count;

        public override IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TVal> filter, FindOptions<TVal, TProjection>? options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            var mockCursor = new Mock<IAsyncCursor<TProjection>>();
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);

            if (filter == FilterDefinition<TVal>.Empty)
            {
#pragma warning disable IDE0305 // Simplify collection initialization
                mockCursor
                    .Setup(x => x.Current)
                    .Returns((IEnumerable<TProjection>)Documents.Values.ToList());
#pragma warning restore IDE0305 // Simplify collection initialization
                return mockCursor.Object;
            }

            var rendered = filter.Render(new RenderArgs<TVal>(BsonSerializer.SerializerRegistry.GetSerializer<TVal>(), BsonSerializer.SerializerRegistry));
            var key = GetKey(rendered["_id"]);

            if (Documents.TryGetValue(key, out var value))
            {
#pragma warning disable IDE0028 // Simplify collection initialization
                mockCursor
                    .Setup(static x => x.Current)
                    .Returns((IEnumerable<TProjection>)new List<TVal> { Documents[key] });
#pragma warning restore IDE0028 // Simplify collection initialization
                return mockCursor.Object;
            }

            mockCursor.Reset();
            mockCursor
                .Setup(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(false);
            return mockCursor.Object;
        }

        public override void InsertOne(TVal document, InsertOneOptions? options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            if (RejectChanges)
                throw new Exception("Rejecting changes");

            BsonDocument bsonDoc = document.ToBsonDocument();
            if (!bsonDoc.Contains("_id"))
                bsonDoc.Add("_id", Guid.NewGuid().ToString());
            if (!Documents.TryAdd(GetKey(bsonDoc["_id"].ToString()!), document))
                throw new Exception("Document already exists");
        }

        public override void InsertMany(IEnumerable<TVal> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (TVal doc in documents)
                InsertOne(doc, null, cancellationToken);
        }

        public override ReplaceOneResult ReplaceOne(FilterDefinition<TVal> filter, TVal replacement,
            ReplaceOptions? options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            if (RejectChanges)
                return ReplaceOneResult.Unacknowledged.Instance;

            BsonDocument bsonDoc = replacement.ToBsonDocument();
            TKey key = GetKey(bsonDoc["_id"].ToString()!);
            if (!Documents.ContainsKey(key))
                return ReplaceOneResult.Unacknowledged.Instance;

            Documents[key] = replacement;
            return new ReplaceOneResult.Acknowledged(1, 1, 0);
        }

        public override DeleteResult DeleteOne(FilterDefinition<TVal> filter, CancellationToken cancellationToken = new CancellationToken())
        {
            if (RejectChanges)
                return DeleteResult.Unacknowledged.Instance;

            var filterDoc = filter.Render(new RenderArgs<TVal>(BsonSerializer.SerializerRegistry.GetSerializer<TVal>(), BsonSerializer.SerializerRegistry));
            if (!filterDoc.Contains("_id") || filterDoc.Count() > 1)
                throw new NotImplementedException("No support for deleting multiple docs");

            return Documents.Remove(GetKey(filterDoc["_id"].ToString()!))
                ? new DeleteResult.Acknowledged(1)
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
            UserName = "testUser",
            Modality = "CT",
            IsIdentifiableExtraction = true,
            IsNoFilterExtraction = true,
        };
        var testHeader = new MessageHeader
        {
            MessageGuid = Guid.NewGuid(),
            OriginalPublishTimestamp = MessageHeader.UnixTime(_dateTimeProvider.UtcNow()),
            Parents = [Guid.NewGuid(),],
            ProducerExecutableName = "MongoExtractStoreTests",
            ProducerProcessID = 1234,
        };

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        store.PersistMessageToStore(testExtractionRequestInfoMessage, testHeader);

        Dictionary<Guid, MongoExtractJobDoc> docs = _extractionDatabase.InProgressCollection.Documents;
        Assert.That(docs, Has.Count.EqualTo(1));
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
            "testUser",
            "CT",
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true,
            null);

        Assert.That(extractJob, Is.EqualTo(expected));
    }

    [Test]
    public void PersistMessageToStoreImpl_ExtractionRequestInfoMessage_CompletedJob()
    {
        // Arrange

        var jobId = Guid.NewGuid();
        var job = new MongoExtractJobDoc(
          jobId,
          MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
          "1234",
          ExtractJobStatus.Failed,
          "test/dir",
          _dateTimeProvider.UtcNow(),
          "SeriesInstanceUID",
          1,
          "testUser",
          "MR",
          isIdentifiableExtraction: true,
          isNoFilterExtraction: true,
          null);

        var client = GetTestMongoClient();
        _extractionDatabase.CompletedJobCollection.InsertOne(new MongoCompletedExtractJobDoc(job, DateTime.Now));

        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
        {
            ExtractionJobIdentifier = jobId,
            ProjectNumber = "1234-5678",
            ExtractionDirectory = "1234-5678/testExtract",
            JobSubmittedAt = _dateTimeProvider.UtcNow(),
            KeyTag = "StudyInstanceUID",
            KeyValueCount = 1,
            UserName = "testUser",
            Modality = "CT",
            IsIdentifiableExtraction = true,
            IsNoFilterExtraction = true,
        };

        // Act

        void call() => store.PersistMessageToStore(testExtractionRequestInfoMessage, new MessageHeader());

        // Assert

        var exc = Assert.Throws<ApplicationException>(() => call());
        Assert.That(exc?.Message, Is.EqualTo("Received an ExtractionRequestInfoMessage for a job that is already completed"));
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
            Parents = [Guid.NewGuid(),],
            ProducerExecutableName = "MongoExtractStoreTests",
            ProducerProcessID = 1234,
        };

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header);

        Dictionary<Guid, MongoExpectedFilesDoc> docs = _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].Documents;
        Assert.That(docs, Has.Count.EqualTo(1));
        MongoExpectedFilesDoc extractJob = docs.Values.ToList()[0];

        var expected = new MongoExpectedFilesDoc(
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
            "series-1",
            [
                new MongoExpectedFileInfoDoc(header1.MessageGuid, "file1"),
                new MongoExpectedFileInfoDoc(header2.MessageGuid, "file2"),
            ],
            new MongoRejectedKeyInfoDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
                new Dictionary<string, int>
                {
                    {"reject1", 1 },
                    {"reject2", 2 },
                })
            );

        Assert.That(extractJob, Is.EqualTo(expected));
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
            ExtractFileMessagesDispatched = [], // No files were extractable for this key
            KeyValue = "series-1",
        };
        var header = new MessageHeader
        {
            MessageGuid = Guid.NewGuid(),
            OriginalPublishTimestamp = MessageHeader.UnixTimeNow(),
            Parents = [Guid.NewGuid(),],
            ProducerExecutableName = "MongoExtractStoreTests",
            ProducerProcessID = 1234,
        };

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        Assert.DoesNotThrow(() => store.PersistMessageToStore(testExtractFileCollectionInfoMessage, header));
    }

    [Test]
    public void TestPersistMessageToStoreImpl_ExtractFileStatusMessage()
    {
        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        Guid jobId = Guid.NewGuid();
        var testExtractFileStatusMessage = new ExtractedFileStatusMessage
        {
            OutputFilePath = "anon.dcm",
            JobSubmittedAt = _dateTimeProvider.UtcNow(),
            Status = ExtractedFileStatus.ErrorWontRetry,
            ProjectNumber = "1234",
            ExtractionJobIdentifier = jobId,
            ExtractionDirectory = "1234/test",
            StatusMessage = "Could not anonymise",
            DicomFilePath = "original.dcm",
        };
        var header = new MessageHeader();

        store.PersistMessageToStore(testExtractFileStatusMessage, header);

        Dictionary<Guid, MongoFileStatusDoc> docs = _extractionDatabase.StatusCollections[$"statuses_{jobId}"].Documents;
        Assert.That(docs, Has.Count.EqualTo(1));
        MongoFileStatusDoc statusDoc = docs.Values.ToList()[0];

        var expected = new MongoFileStatusDoc(
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
            "original.dcm",
            "anon.dcm",
            ExtractedFileStatus.ErrorWontRetry,
            VerifiedFileStatus.NotVerified,
            "Could not anonymise");

        Assert.That(statusDoc, Is.EqualTo(expected));
    }

    [Test]
    public void PersistMessageToStoreImpl_ExtractedFileVerificationMessage_CompletedJob()
    {
        // Arrange

        var jobId = Guid.NewGuid();
        var job = new MongoExtractJobDoc(
          jobId,
          MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
          "1234",
          ExtractJobStatus.Failed,
          "test/dir",
          _dateTimeProvider.UtcNow(),
          "SeriesInstanceUID",
          1,
          "testUser",
          "MR",
          isIdentifiableExtraction: true,
          isNoFilterExtraction: true,
          null);

        var client = GetTestMongoClient();
        _extractionDatabase.CompletedJobCollection.InsertOne(new MongoCompletedExtractJobDoc(job, DateTime.Now));

        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        var extractedFileStatusMessage = new ExtractedFileVerificationMessage()
        {
            ExtractionJobIdentifier = jobId,
            OutputFilePath = "foo-an.dcm",
            Report = "[]",
        };

        // Act

        void call() => store.PersistMessageToStore(extractedFileStatusMessage, new MessageHeader());

        // Assert

        var exc = Assert.Throws<ApplicationException>(() => call());
        Assert.That(exc?.Message, Is.EqualTo($"Received an {nameof(ExtractedFileVerificationMessage)} for a job that is already completed"));
    }


    [Test]
    public void TestPersistMessageToStoreImpl_IsIdentifiableMessage()
    {
        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        Guid jobId = Guid.NewGuid();
        var testIsIdentifiableMessage = new ExtractedFileVerificationMessage
        {
            OutputFilePath = "anon.dcm",
            JobSubmittedAt = _dateTimeProvider.UtcNow(),
            ProjectNumber = "1234",
            ExtractionJobIdentifier = jobId,
            ExtractionDirectory = "1234/test",
            DicomFilePath = "original.dcm",
            Status = VerifiedFileStatus.NotIdentifiable,
            Report = "[]", // NOTE(rkm 2020-03-10) An "empty" report from IsIdentifiable
        };
        var header = new MessageHeader();

        store.PersistMessageToStore(testIsIdentifiableMessage, header);

        Dictionary<Guid, MongoFileStatusDoc> docs = _extractionDatabase.StatusCollections[$"statuses_{jobId}"].Documents;
        Assert.That(docs, Has.Count.EqualTo(1));
        MongoFileStatusDoc statusDoc = docs.Values.ToList()[0];

        var expected = new MongoFileStatusDoc(
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, header, _dateTimeProvider),
            "original.dcm",
            "anon.dcm",
            ExtractedFileStatus.Anonymised,
            VerifiedFileStatus.NotIdentifiable,
            "[]");

        Assert.That(statusDoc, Is.EqualTo(expected));
    }

    [Test]
    public void TestGetReadJobsImpl()
    {
        var jobId = Guid.NewGuid();
        var testJob = new MongoExtractJobDoc(
            jobId,
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
            "1234",
            ExtractJobStatus.Failed,
            "test/dir",
            _dateTimeProvider.UtcNow(),
            "SeriesInstanceUID",
            1,
            "testUser",
            "MR",
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true,
            null);
        var testMongoExpectedFilesDoc = new MongoExpectedFilesDoc(
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
            "1.2.3.4",
            [
                new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon1.dcm"),
            ],
            new MongoRejectedKeyInfoDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                [])
        );
        var testMongoFileStatusDoc = new MongoFileStatusDoc(
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
            "input.dcm",
            "anon1.dcm",
            ExtractedFileStatus.Anonymised,
            VerifiedFileStatus.NotIdentifiable,
            "Verified");

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        // Assert that jobs marked as failed are not returned
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.InProgressCollection.RejectChanges = true;
        Assert.That(store.GetReadyJobs(), Is.Empty);

        // Assert that an in progress job is not returned
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.InProgressCollection.RejectChanges = true;
        Assert.That(store.GetReadyJobs(), Is.Empty);

        // Check we handle a bad ReplaceOneResult
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.InProgressCollection.RejectChanges = true;
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
        Assert.Throws<ApplicationException>(() => store.GetReadyJobs());

        // Check happy path
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
        Assert.Multiple(() =>
        {
            Assert.That(store.GetReadyJobs(), Is.Empty);
            Assert.That(_extractionDatabase.InProgressCollection.Documents.Single().Value.JobStatus, Is.EqualTo(ExtractJobStatus.WaitingForStatuses));
        });
        _extractionDatabase.StatusCollections[$"statuses_{jobId}"] = new MockExtractCollection<Guid, MongoFileStatusDoc>();
        _extractionDatabase.StatusCollections[$"statuses_{jobId}"].InsertOne(testMongoFileStatusDoc);
        ExtractJobInfo job = store.GetReadyJobs().Single();
        Assert.Multiple(() =>
        {
            Assert.That(job.JobStatus, Is.EqualTo(ExtractJobStatus.ReadyForChecks));
            Assert.That(_extractionDatabase.InProgressCollection.Documents.Single().Value.JobStatus, Is.EqualTo(ExtractJobStatus.ReadyForChecks));
        });
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
            "testUser",
            "MR",
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true,
            null);
        var testMongoExpectedFilesDoc = new MongoExpectedFilesDoc(
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
            "1.2.3.4",
            [
                new MongoExpectedFileInfoDoc(Guid.NewGuid(), "anon1.dcm"),
            ],
            new MongoRejectedKeyInfoDoc(
                MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
                [])
            );
        var testMongoFileStatusDoc = new MongoFileStatusDoc(
            MongoExtractionMessageHeaderDoc.FromMessageHeader(jobId, new MessageHeader(), _dateTimeProvider),
            "input.dcm",
            "anon1.dcm",
            ExtractedFileStatus.Anonymised,
            VerifiedFileStatus.NotIdentifiable,
            "Verified");

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        // Assert that an exception is thrown for a non-existent job
        Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(Guid.NewGuid()));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Assert that an exception is thrown for a job which is marked as failed
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _mockSessionHandle.Reset();
        Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(Guid.NewGuid()));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Check that we handle a failed insertion
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        testJob.JobStatus = ExtractJobStatus.Completed;
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.CompletedJobCollection.RejectChanges = true;
        _mockSessionHandle.Reset();
        Assert.Throws<Exception>(() => store.MarkJobCompleted(jobId));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Check we handle a bad DeleteResult
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        _extractionDatabase.InProgressCollection.RejectChanges = true;
        _mockSessionHandle.Reset();
        Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(jobId));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Check we handle missing expectedFiles collection
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _mockSessionHandle.Reset();
        Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(jobId));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Check we handle missing statuses collection
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
        _mockSessionHandle.Reset();
        Assert.Throws<ApplicationException>(() => store.MarkJobCompleted(jobId));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Check happy path
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"] = new MockExtractCollection<Guid, MongoExpectedFilesDoc>();
        _extractionDatabase.ExpectedFilesCollections[$"expectedFiles_{jobId}"].InsertOne(testMongoExpectedFilesDoc);
        _extractionDatabase.StatusCollections[$"statuses_{jobId}"] = new MockExtractCollection<Guid, MongoFileStatusDoc>();
        _extractionDatabase.StatusCollections[$"statuses_{jobId}"].InsertOne(testMongoFileStatusDoc);
        _mockSessionHandle.Reset();
        store.MarkJobCompleted(jobId);
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Never);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(_extractionDatabase.ExpectedFilesCollections, Has.Count.EqualTo(1));
            Assert.That(_extractionDatabase.StatusCollections, Has.Count.EqualTo(1));
        });
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
            "testUser",
            "MR",
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true,
            null);

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        // Assert that an exception is thrown for a non-existent job
        Assert.Throws<ApplicationException>(() => store.MarkJobFailed(Guid.NewGuid(), new Exception()));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Assert that a job can't be failed twice
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _mockSessionHandle.Reset();
        Assert.Throws<ApplicationException>(() => store.MarkJobFailed(jobId, new Exception()));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Check we handle a bad ReplaceOneResult
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _extractionDatabase.InProgressCollection.RejectChanges = true;
        _mockSessionHandle.Reset();
        Assert.Throws<ApplicationException>(() => store.MarkJobFailed(jobId, new Exception()));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);

        // Check happy path
        client = GetTestMongoClient();
        store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);
        testJob.JobStatus = ExtractJobStatus.WaitingForCollectionInfo;
        testJob.FailedJobInfoDoc = null;
        _extractionDatabase.InProgressCollection.InsertOne(testJob);
        _mockSessionHandle.Reset();
        store.MarkJobFailed(jobId, new Exception("TestMarkJobFailedImpl"));
        _mockSessionHandle.Verify(x => x.AbortTransaction(It.IsAny<CancellationToken>()), Times.Never);
        _mockSessionHandle.Verify(x => x.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
        Dictionary<Guid, MongoExtractJobDoc> docs = _extractionDatabase.InProgressCollection.Documents;
        Assert.That(docs, Has.Count.EqualTo(1));
        MongoExtractJobDoc failedDoc = docs[jobId];
        Assert.Multiple(() =>
        {
            Assert.That(failedDoc.JobStatus, Is.EqualTo(ExtractJobStatus.Failed));
            Assert.That(failedDoc.FailedJobInfoDoc, Is.Not.Null);
        });
        Assert.That(failedDoc.FailedJobInfoDoc!.ExceptionMessage, Is.EqualTo("TestMarkJobFailedImpl"));
    }

    [Test]
    public void AddToWriteQueue_ProcessVerificationMessageQueue()
    {
        // Arrange

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        Guid jobId = Guid.NewGuid();
        var message = new ExtractedFileVerificationMessage
        {
            OutputFilePath = "anon.dcm",
            JobSubmittedAt = _dateTimeProvider.UtcNow(),
            ProjectNumber = "1234",
            ExtractionJobIdentifier = jobId,
            ExtractionDirectory = "1234/test",
            DicomFilePath = "original.dcm",
            Status = VerifiedFileStatus.NotIdentifiable,
            Report = "[]",
        };
        var header = new MessageHeader();

        var nMessages = 10;

        // Act

        for (int i = 0; i < nMessages; ++i)
            store.AddToWriteQueue(message, header, (ulong)i);

        store.ProcessVerificationMessageQueue();

        // Assert

        Assert.That(
            _extractionDatabase.StatusCollections[$"statuses_{jobId}"].Documents, Has.Count
.EqualTo(nMessages));
    }

    [Test]
    public void ProcessVerificationMessageQueue_Empty()
    {
        // Arrange

        var client = GetTestMongoClient();
        var store = new MongoExtractJobStore(client, ExtractionDatabaseName, _dateTimeProvider);

        // Act
        store.ProcessVerificationMessageQueue();

        // Assert
        // No exception
    }

    #endregion
}
