using FellowOakDicom;
using DicomTypeTranslation;
using MongoDB.Bson;
using NUnit.Framework;
using SmiServices.Microservices.DicomReprocessor;
using SmiServices.Common.Messaging;
using SmiServices.Common.MongoDB;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using Moq;

namespace SmiServices.UnitTests.Microservices.DicomReprocessor
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
        public void ProcessDocument_NationalPacsAccessionNumber_IsIgnored()
        {
            var processor = new DicomFileProcessor(new DicomReprocessorOptions(), Mock.Of<IProducerModel>(), "");

            var msg = new DicomFileMessage
            {
                DicomFilePath = "foo",
                DicomFileSize = 123,
            };
            BsonDocument bsonHeader = MongoDocumentHeaders.ImageDocumentHeader(msg, new MessageHeader());
            bsonHeader.Add("NationalPACSAccessionNumber", "foo");
            BsonDocument datasetDoc = DicomTypeTranslaterReader.BuildBsonDocument(new DicomDataset());

            BsonDocument document = new BsonDocument()
                .Add("_id", "foo")
                .Add("header", bsonHeader)
                .AddRange(datasetDoc);

            processor.ProcessDocument(document);
        }

        #endregion
    }
}
