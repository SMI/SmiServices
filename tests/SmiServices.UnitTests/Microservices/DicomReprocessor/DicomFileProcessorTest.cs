using DicomTypeTranslation;
using MongoDB.Bson;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.MongoDB;
using SmiServices.Microservices.DicomReprocessor;
using SmiServices.UnitTests.Common.Messaging;

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
            var processor = new DicomFileProcessor(new TestProducer<DicomFileMessage>(), "");

            var msg = new DicomFileMessage
            {
                DicomFilePath = "foo",
                DicomFileSize = 123,
            };
            BsonDocument bsonHeader = MongoDocumentHeaders.ImageDocumentHeader(msg, new MessageHeader());
            bsonHeader.Add("NationalPACSAccessionNumber", "foo");
            BsonDocument datasetDoc = DicomTypeTranslaterReader.BuildBsonDocument([]);

            BsonDocument document = new BsonDocument()
                .Add("_id", "foo")
                .Add("header", bsonHeader)
                .AddRange(datasetDoc);

            processor.ProcessDocument(document);
        }

        #endregion
    }
}
