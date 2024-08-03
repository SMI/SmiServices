using FellowOakDicom;
using DicomTypeTranslation;
using Microservices.DicomReprocessor.Execution.Processors;
using MongoDB.Bson;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using Moq;
using Smi.Common.Messaging;

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
