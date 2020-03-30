using System;
using System.Diagnostics;
using System.IO;
using Dicom;
using DicomTypeTranslation;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Runners;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using Smi.Common.Tests;

namespace Microservices.IsIdentifiable.Tests.RunnerTests
{
    [RequiresMongoDb]
    public class MongoRunnerTests
    {
        #region Fixture Methods 

        private IsIdentifiableMongoOptions _options;
        private const string DB_NAME = "IsIdentifiableTests";
        private const string TEST_DATA_COLL_NAME = "testData";

        private string _appId;

        //TODO Make these configurable if needed
        private const string DEFAULT_HOSTNAME = "localhost";
        private const int DEFAULT_PORT = 27017;

        private IMongoDatabase _testDatabase;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Process process = Process.GetCurrentProcess();
            _appId = process.ProcessName + "-" + process.Id;

            // TODO Pull out common parts of test setup/teardown into separate class that all runner tests can use

            _options = new IsIdentifiableMongoOptions()
            {
                HostName = DEFAULT_HOSTNAME,
                Port = DEFAULT_PORT,
                DatabaseName = "IsIdentifiableTests",
                
                StoreReport = true,
                TreeReport = true,

                DestinationCsvFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestReports")
            };

            var mongoClient = new MongoClient(new MongoClientSettings
            {
                ApplicationName = _appId,
                Server = new MongoServerAddress(DEFAULT_HOSTNAME, DEFAULT_PORT)
            });

            _testDatabase = mongoClient.GetDatabase(DB_NAME);
            CreateTestData(_testDatabase);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _testDatabase
            .ListCollectionNames()
            .ToList()
            .ForEach(collName =>
            {
                if (collName != TEST_DATA_COLL_NAME)
                    _testDatabase.DropCollection(collName);
            });
        }

        private static void CreateTestData(IMongoDatabase testDatabase)
        {
            IMongoCollection<BsonDocument> testCollection = testDatabase.GetCollection<BsonDocument>(TEST_DATA_COLL_NAME);

            // Only recreate the test data if we need to
            if (testCollection.CountDocuments(FilterDefinition<BsonDocument>.Empty) != 0)
                return;

            var f = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "mydicom.dcm"));
            TestData.Create(f,TestData.IMG_013);

            DicomDataset testDataset = DicomFile.Open(f.FullName).Dataset;

            testDataset.Remove(DicomTag.PixelData);
            testDataset.AddOrUpdate(DicomTag.PatientName, "Smith^David");

            BsonDocument datasetDoc = DicomTypeTranslaterReader.BuildBsonDocument(testDataset);

            // Add our header information
            var header = new BsonDocument
            {
                {"NationalPACSAccessionNumber", "test-NationalPACSAccessionNumber"},
                {"DicomFilePath",               "path/to/file.dcm"},
                {"StudyInstanceUID",            "test-StudyInstanceUID"},
                {"SeriesInstanceUID",           "test-SeriesInstanceUID"},
                {"SOPInstanceUID",              "test-SOPInstanceUID"}
            };

            BsonDocument testDocument = new BsonDocument()
                .Add("header", header)
                .AddRange(datasetDoc);

            for (var i = 0; i < 50; ++i)
                testCollection.InsertOne(new BsonDocument(testDocument));
        }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        private void SetupTestCollection(string testName)
        {
            string testCollName = "RunnerTests_MongoRunnerTests_" + testName + DateTime.Now.Ticks;
            _options.CollectionName = testCollName;

            // Copy the source data to the collection for testing
            _testDatabase
            .GetCollection<BsonDocument>(testCollName)
            .InsertMany(
                _testDatabase
                .GetCollection<BsonDocument>(TEST_DATA_COLL_NAME)
                .Find(FilterDefinition<BsonDocument>.Empty)
                .ToList()
            );
        }

        #endregion

        #region Tests

        [Test]
        public void TestBasic()
        {
            SetupTestCollection("TestBasic");

            var mongoRunner = new IsIdentifiableMongoRunner(_options, _appId);
            mongoRunner.Run();
        }

        #endregion
    }
}
