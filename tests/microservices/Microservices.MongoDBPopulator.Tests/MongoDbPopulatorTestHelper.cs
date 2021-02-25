
using Dicom;
using DicomTypeTranslation;
using MongoDB.Driver;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using System;


namespace Microservices.MongoDBPopulator.Tests
{
    public class MongoDbPopulatorTestHelper
    {
        private const string TestDbName = "nUnitTests";

        private MongoClient _mongoTestClient;

        public IMongoDatabase TestDatabase;

        public GlobalOptions Globals;

        public DicomFileMessage TestImageMessage;
        public SeriesMessage TestSeriesMessage;

        public void SetupSuite()
        {
            Globals = GetNewMongoDbPopulatorOptions();

            _mongoTestClient = MongoClientHelpers.GetMongoClient(Globals.MongoDatabases.DicomStoreOptions, "MongoDbPopulatorTests");

            _mongoTestClient.DropDatabase(TestDbName);
            TestDatabase = _mongoTestClient.GetDatabase(TestDbName);

            Globals.MongoDbPopulatorOptions.SeriesQueueConsumerOptions = new ConsumerOptions()
            {
                QueueName = "TEST.SeriesQueue",
                QoSPrefetchCount = 5,
                AutoAck = false
            };

            Globals.MongoDbPopulatorOptions.ImageQueueConsumerOptions = new ConsumerOptions()
            {
                QueueName = "TEST.MongoImageQueue",
                QoSPrefetchCount = 50,
                AutoAck = false
            };

            var dataset = new DicomDataset
            {
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3.4"),
                new DicomCodeString(DicomTag.Modality, "SR")
            };

            string serialized = DicomTypeTranslater.SerializeDatasetToJson(dataset);

            TestImageMessage = new DicomFileMessage
            {
                DicomFilePath = "Path/To/File",
                NationalPACSAccessionNumber = "123",
                SeriesInstanceUID = "TestSeriesInstanceUID",
                StudyInstanceUID = "TestStudyInstanceUID",
                SOPInstanceUID = "TestSOPInstanceUID",
                DicomDataset = serialized
            };

            TestSeriesMessage = new SeriesMessage
            {
                DirectoryPath = "Path/To/Series",
                ImagesInSeries = 123,
                NationalPACSAccessionNumber = "123",
                SeriesInstanceUID = "TestSeriesInstanceUID",
                StudyInstanceUID = "TestStudyInstanceUID",
                DicomDataset = serialized
            };
        }

        public static GlobalOptions GetNewMongoDbPopulatorOptions()
        {
            GlobalOptions options = new GlobalOptionsFactory().Load();

            options.MongoDatabases.DicomStoreOptions.DatabaseName = TestDbName;
            options.MongoDbPopulatorOptions.MongoDbFlushTime = 1; //1 second

            return options;
        }

        public static string GetCollectionNameForTest(string testName) => testName + "-" + Guid.NewGuid();

        public void Dispose()
        {
            _mongoTestClient.DropDatabase(TestDbName);
        }
    }
}
