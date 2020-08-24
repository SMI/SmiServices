using Dicom;
using DicomTypeTranslation;
using Microservices.DicomReprocessor.Execution.Processors;
using MongoDB.Bson;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.MongoDB;
using Smi.Common.Options;


namespace Microservices.DicomReprocessor.Tests.Execution.Processors
{
    public class DicomFileProcessorTest
    {
        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp() { }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void Test_DicomFileProcessor_ProcessDocument_NullAccNo()
        {
            var processor = new DicomFileProcessor(new DicomReprocessorOptions(), null, null);

            BsonDocument datasetDoc = DicomTypeTranslaterReader.BuildBsonDocument(new DicomDataset());
            var msg = new DicomFileMessage
            {
                DicomFilePath = "foo",
                DicomFileSize = 123,
                NationalPACSAccessionNumber = null,
            };
            datasetDoc.Add("_id", "foo");
            BsonDocument bsonHeader = MongoDocumentHeaders.ImageDocumentHeader(msg, new MessageHeader());
            BsonDocument document = new BsonDocument()
                .Add("header", bsonHeader)
                .AddRange(datasetDoc);

            processor.ProcessDocument(document);
        }

        #endregion
    }
}
