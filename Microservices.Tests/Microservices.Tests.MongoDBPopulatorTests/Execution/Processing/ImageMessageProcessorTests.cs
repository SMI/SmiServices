
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dicom;
using DicomTypeTranslation;
using Microservices.Common.Messages;
using Microservices.Common.Options;
using Microservices.Common.Tests;
using Microservices.MongoDBPopulator.Execution;
using Microservices.MongoDBPopulator.Execution.Processing;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Tests.Common.Smi;


namespace Microservices.Tests.MongoDBPopulatorTests.Execution.Processing
{
    [TestFixture, RequiresMongoDb]
    public class ImageMessageProcessorTests
    {
        private MongoDbPopulatorTestHelper _helper;

        private readonly List<string> _imageMessageProps = typeof(DicomFileMessage).GetProperties().Select(x => x.Name).ToList();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _helper = new MongoDbPopulatorTestHelper();
            _helper.SetupSuite();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _helper.Dispose();
        }

        private bool Validate(DicomFileMessage message, BsonDocument document)
        {
            if (message == null || document == null)
                throw new ArgumentException("Either message or document was null");

            if (message.DicomDataset == null)
                throw new ArgumentException("Dataset in message was null");

            if (document.ElementCount == 0)
                throw new ArgumentException("Document did not contain any elements");

            BsonElement element;
            if (!document.TryGetElement("header", out element))
                throw new ArgumentException("Document did not contain a header element");

            if (!(element.Value is BsonDocument))
                throw new ArgumentException("Documents header value was not a sub-document");

            DicomDataset dataset = DicomTypeTranslater.DeserializeJsonToDataset(message.DicomDataset);

            if (dataset == null)
                throw new ArgumentException("Deserialized dataset was null");

            BsonDocument datasetDocument = DicomTypeTranslaterReader.BuildBsonDocument(dataset);
            document.Remove("_id");
            document.Remove("header");

            bool headerOk = ValidateHeader(message, (BsonDocument)element.Value);
            bool bodyOk = datasetDocument.Equals(document);

            return headerOk && bodyOk;
        }

        private bool ValidateHeader(DicomFileMessage message, BsonDocument header)
        {
            if (!header.All(x => _imageMessageProps.Contains(x.Name)))
                throw new ArgumentException("document header did not contain all the required elements");

            var isOk = true;

            isOk &= message.NationalPACSAccessionNumber == header["NationalPACSAccessionNumber"];
            isOk &= message.DicomFilePath == header["DicomFilePath"];
            isOk &= message.StudyInstanceUID == header["StudyInstanceUID"];
            isOk &= message.SeriesInstanceUID == header["SeriesInstanceUID"];
            isOk &= message.SOPInstanceUID == header["SOPInstanceUID"];

            return isOk;
        }

        /// <summary>
        /// Tests that we timeout and throw an exception if we lose MongoDb connection after startup
        /// </summary>
        [Test]
        public void TestLossOfMongoConnection()
        {
            Assert.Fail("Need to test this in some other way");

            _helper.Globals.MongoDbPopulatorOptions.MongoDbFlushTime = 1;
            _helper.Globals.MongoDbPopulatorOptions.FailedWriteLimit = 1;

            var mockAdapter = new Mock<IMongoDbAdapter>();
            //mockAdapter.SetupSequence(x => x.Ping(It.IsAny<int>()))
            //    .Pass()
            //    .Throws(new ApplicationException("Mocked loss of connection"));

            var callbackUsed = false;
            Action<Exception> ExceptionCallback = (exception) => { callbackUsed = true; };

            var processor = new SeriesMessageProcessor(_helper.Globals.MongoDbPopulatorOptions, mockAdapter.Object, 1, ExceptionCallback);

            Assert.True(processor.IsProcessing);

            Thread.Sleep((_helper.Globals.MongoDbPopulatorOptions.MongoDbFlushTime * 1000) * _helper.Globals.MongoDbPopulatorOptions.FailedWriteLimit + 2000);

            Assert.False(processor.IsProcessing);
            Assert.True(callbackUsed);
        }

        /// <summary>
        /// Write a single image message and test the document format is as expected
        /// </summary>
        [Test]
        public void TestImageDocumentFormat()
        {
            GlobalOptions options = _helper.GetNewMongoDbPopulatorOptions();
            options.MongoDbPopulatorOptions.MongoDbFlushTime = int.MaxValue / 1000;

            string collectionName = _helper.GetCollectionNameForTest("TestImageDocumentFormat");
            var testAdapter = new MongoDbAdapter("TestImageDocumentFormat", options.MongoDatabases.DicomStoreOptions, collectionName);

            var callbackUsed = false;
            Action<Exception> ExceptionCallback = (exception) => { callbackUsed = true; };

            var processor = new ImageMessageProcessor(options.MongoDbPopulatorOptions, testAdapter, 1, ExceptionCallback)
            {
                Model = Mock.Of<IModel>()
            };

            // Max queue size set to 1 so will immediately process this
            processor.AddToWriteQueue(_helper.TestImageMessage, new MessageHeader(), 1);

            Assert.False(callbackUsed);
            Assert.True(processor.AckCount == 1);

            IMongoCollection<BsonDocument> imageCollection = _helper.TestDatabase.GetCollection<BsonDocument>(collectionName + "_SR");

            Assert.True(imageCollection.CountDocuments(new BsonDocument()) == 1);
        }


        [Test]
        public void TestLargeMessageNack()
        {
            GlobalOptions options = _helper.GetNewMongoDbPopulatorOptions();
            options.MongoDbPopulatorOptions.MongoDbFlushTime = int.MaxValue / 1000;

            var adapter = new MongoDbAdapter("ImageProcessor", options.MongoDatabases.ExtractionStoreOptions, "largeDocumentTest");
            var processor = new ImageMessageProcessor(options.MongoDbPopulatorOptions, adapter, 1, null);
            var mockModel = Mock.Of<IModel>();
            processor.Model = mockModel;

            var dataset = new DicomDataset
            {
                {DicomTag.SelectorLOValue, new string('x', 16*1024*1024)}
            };

            string json = DicomTypeTranslater.SerializeDatasetToJson(dataset);

            var largeMessage = new DicomFileMessage
            {
                SeriesInstanceUID = "",
                StudyInstanceUID = "",
                SOPInstanceUID = "",
                NationalPACSAccessionNumber = "",
                DicomFilePath = "",
                DicomDataset = json
            };

            Assert.Throws<ApplicationException>(() => processor.AddToWriteQueue(largeMessage, new MessageHeader(), 1));

            dataset = new DicomDataset
            { 
                // Should be ok, getting close to the threshold
                {DicomTag.SelectorLOValue, new string('x', 15*1024*1024 + 512)}
            };

            json = DicomTypeTranslater.SerializeDatasetToJson(dataset);
            largeMessage.DicomDataset = json;

            processor.AddToWriteQueue(largeMessage, new MessageHeader(), 2);
            Assert.True(processor.AckCount == 1);
        }

        [Test]
        public void TestLargeDocumentSplitOk()
        {
            GlobalOptions options = _helper.GetNewMongoDbPopulatorOptions();
            options.MongoDbPopulatorOptions.MongoDbFlushTime = int.MaxValue / 1000;

            var adapter = new MongoDbAdapter("ImageProcessor", options.MongoDatabases.ExtractionStoreOptions, "largeDocumentTest");
            var processor = new ImageMessageProcessor(options.MongoDbPopulatorOptions, adapter, 2, null);
            var mockModel = Mock.Of<IModel>();
            processor.Model = mockModel;

            var dataset = new DicomDataset
            {
                {DicomTag.SelectorLOValue, new string('x', 10*1024*1024)}
            };

            var largeMessage = new DicomFileMessage
            {
                SeriesInstanceUID = "",
                StudyInstanceUID = "",
                SOPInstanceUID = "",
                NationalPACSAccessionNumber = "",
                DicomFilePath = "",
                DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(dataset)
            };

            processor.AddToWriteQueue(largeMessage, new MessageHeader(), 1);
            processor.AddToWriteQueue(largeMessage, new MessageHeader(), 2);
        }
    }
}
