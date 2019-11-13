
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDocuments;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;

namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    [TestFixture, RequiresMongoDb]
    public class MongoExtractStoreTests
    {
        private const string JobCollectionName = "extractJobStore";

        private MongoClient _mongoClient;
        private string _testDbName;
        private IMongoDatabase _testJobDatabase;
        private IMongoCollection<MongoExtractJob> _testExtractJobStoreCollection;

        private readonly GlobalOptions _globalOptions = GlobalOptions.Load("default.yaml", TestContext.CurrentContext.TestDirectory);
        private MongoDbOptions _mongoDbOptions;

        private readonly Guid _extractionIdentifier = Guid.NewGuid();
        private readonly Guid _messageHeaderGuid = Guid.NewGuid();
        private DateTime _testTime;

        private ExtractionRequestInfoMessage _testExtractionRequestInfoMessage;
        private IMessageHeader _testExtractionRequestInfoMessageHeader;

        private ExtractFileCollectionInfoMessage _testFileCollectionInfoMessage;
        private IMessageHeader _testFileCollectionInfoMessageHeader;

        private ExtractFileStatusMessage _testStatusMessageOk;
        private ExtractFileStatusMessage _testStatusMessageWillRetry;
        private ExtractFileStatusMessage _testStatusMessageWontRetry;
        private IMessageHeader _testStatusMessageHeader;

        private ExtractJobInfo _expectedBasicJobInfo;

        #region Fixture Methods 

        /// <summary> 
        /// Run once before any tests in this fixture 
        /// </summary> 
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _mongoDbOptions = _globalOptions.MongoDatabases.ExtractionStoreOptions;
            _testDbName = _mongoDbOptions.DatabaseName;

            _testTime = DateTime.Now;

            _mongoClient = MongoClientHelpers.GetMongoClient(_mongoDbOptions, "CohortPackagerTests");

            _mongoClient.DropDatabase(_testDbName);
            _testJobDatabase = _mongoClient.GetDatabase(_testDbName);
            _testExtractJobStoreCollection = _testJobDatabase.GetCollection<MongoExtractJob>(JobCollectionName);

            _testExtractionRequestInfoMessageHeader = Mock.Of<IMessageHeader>(x =>
            x.MessageGuid == _messageHeaderGuid
            && x.ProducerExecutableName == "MockedLinkerCL"
            && x.ProducerProcessID == 123);

            _testFileCollectionInfoMessageHeader = Mock.Of<IMessageHeader>(x =>
            x.MessageGuid == _messageHeaderGuid
            && x.ProducerExecutableName == "MockedCohortExtractor"
            && x.ProducerProcessID == 123);

            _testStatusMessageHeader = Mock.Of<IMessageHeader>(x =>
            x.MessageGuid == _messageHeaderGuid
            && x.ProducerExecutableName == "MockedAnonymiser"
            && x.ProducerProcessID == 123);
        }

        /// <summary> 
        /// Run once after every test in this fixture 
        /// </summary> 
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _mongoClient.DropDatabase(_testDbName);
        }

        #endregion

        #region Test Methods 

        /// <summary> 
        /// Run once before every test in this fixture 
        /// </summary> 
        [SetUp]
        public void SetUp()
        {
            _mongoClient.DropDatabase(_testDbName);

            _testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionJobIdentifier = _extractionIdentifier,
                ProjectNumber = "1234-5678",
                ExtractionDirectory = @"1234-5678\testExtract",
                JobSubmittedAt = _testTime,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 1
            };

            var dispatched = new JsonCompatibleDictionary<MessageHeader, string>
            {
                {new MessageHeader(), "file1.dcm"},
                {new MessageHeader(), "file2.dcm"},
                {new MessageHeader(), "file3.dcm"}
            };

            _testFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                ExtractionJobIdentifier = _extractionIdentifier,
                ExtractFileMessagesDispatched = dispatched,
                KeyValue = "1.2.3.4"
            };

            _testStatusMessageOk = new ExtractFileStatusMessage
            {
                ExtractionJobIdentifier = _extractionIdentifier,

                AnonymisedFileName = "file1.dcm",
                Status = ExtractFileStatus.Anonymised,
                StatusMessage = null
            };

            _testStatusMessageWillRetry = new ExtractFileStatusMessage
            {
                ExtractionJobIdentifier = _extractionIdentifier,
                AnonymisedFileName = null,
                Status = ExtractFileStatus.ErrorWillRetry,
                StatusMessage = "Not giving up!"
            };
            _testStatusMessageWontRetry = new ExtractFileStatusMessage
            {
                ExtractionJobIdentifier = _extractionIdentifier,
                AnonymisedFileName = null,
                Status = ExtractFileStatus.ErrorWontRetry,
                StatusMessage = "Oh fish :("
            };

            var extractFileCollectionInfos = new List<ExtractFileCollectionInfo>
            {
                new ExtractFileCollectionInfo("1.2.3.4", new List<string>
                {
                    "file1.dcm", "file2.dcm", "file3.dcm"
                })
            };

            var fileStatusInfoOk = new ExtractFileStatusInfo("Anonymised", "file1.dcm", null);
            var fileStatusInfoErrorWillRetry = new ExtractFileStatusInfo("ErrorWillRetry", null, "Not giving up!");
            var fileStatusInfoErrorWontRetry = new ExtractFileStatusInfo("ErrorWontRetry", null, "Oh fish :(");

            _expectedBasicJobInfo = new ExtractJobInfo(_extractionIdentifier, "1234-5678", _testTime, ExtractJobStatus.WaitingForFiles, @"1234-5678\testExtract", 1, "SeriesInstanceUID", extractFileCollectionInfos, new List<ExtractFileStatusInfo> { fileStatusInfoOk, fileStatusInfoErrorWillRetry, fileStatusInfoErrorWontRetry });
        }

        /// <summary> 
        /// Run once after every test in this fixture 
        /// </summary> 
        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests 

        [Test]
        public void TestPersistJobInfoToStore_Basic()
        {
            var store = new MongoExtractJobStore(_mongoDbOptions);

            store.PersistMessageToStore(_testExtractionRequestInfoMessage, _testExtractionRequestInfoMessageHeader);
            store.PersistMessageToStore(_testFileCollectionInfoMessage, _testFileCollectionInfoMessageHeader);
            store.PersistMessageToStore(_testStatusMessageOk, _testStatusMessageHeader);
            store.PersistMessageToStore(_testStatusMessageWillRetry, _testStatusMessageHeader);
            store.PersistMessageToStore(_testStatusMessageWontRetry, _testStatusMessageHeader);

            long docCount = _testExtractJobStoreCollection.CountDocuments(FilterDefinition<MongoExtractJob>.Empty);
            Assert.AreEqual(1, docCount);

            docCount = _testJobDatabase
                .GetCollection<MongoExtractJob>("statuses_" + _extractionIdentifier)
                .CountDocuments(FilterDefinition<MongoExtractJob>.Empty);

            Assert.AreEqual(3, docCount);
        }

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
            IMongoCollection<ArchivedMongoExtractJob> archiveCollection = _testJobDatabase.GetCollection<ArchivedMongoExtractJob>("extractJobArchive");

            store.CleanupJobData(_extractionIdentifier);

            long jobStoreCount = _testExtractJobStoreCollection.CountDocuments(FilterDefinition<MongoExtractJob>.Empty);
            long archiveCount = archiveCollection.CountDocuments(FilterDefinition<ArchivedMongoExtractJob>.Empty);

            Assert.AreEqual(0, jobStoreCount);
            Assert.AreEqual(1, archiveCount);

            ArchivedMongoExtractJob archivedJob = archiveCollection.Find(FilterDefinition<ArchivedMongoExtractJob>.Empty).Single();

            DateTime now = DateTime.UtcNow;
            Assert.True(now - archivedJob.ArchivedAt < TimeSpan.FromSeconds(5));
            Assert.True(archivedJob.JobStatus == ExtractJobStatus.Archived);
        }

#if false
        [Test]
        public void TestQuarantine()
        {
            var store = new MongoExtractJobStore(_mongoDbOptions);
            IMongoCollection<BsonDocument> quarantineCollection = _testJobDatabase.GetCollection<BsonDocument>("extractJobQuarantine");

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
            IMongoCollection<BsonDocument> quarantineCollection = _testJobDatabase.GetCollection<BsonDocument>("extractJobQuarantine");

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
