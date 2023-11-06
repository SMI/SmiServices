
using FellowOakDicom;
using DicomTypeTranslation;
using Microservices.MongoDBPopulator.Execution;
using Microservices.MongoDBPopulator.Execution.Processing;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using Smi.Common.Messages;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Microservices.MongoDBPopulator.Tests.Execution.Processing
{
    [TestFixture, RequiresMongoDb]
    public class ImageMessageProcessorTests
    {
        private MongoDbPopulatorTestHelper _helper = null!;

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

        private void Validate(DicomFileMessage message, MessageHeader header, BsonDocument document)
        {
            Assert.True(document.TryGetElement("header", out var element));

            var docHeader = (BsonDocument)element.Value;
            Assert.AreEqual(_imageMessageProps.Count - 3, docHeader.ElementCount);
            ValidateHeader(message, header, docHeader);

            DicomDataset dataset = DicomTypeTranslater.DeserializeJsonToDataset(message.DicomDataset);
            Assert.NotNull(dataset);

            BsonDocument datasetDocument = DicomTypeTranslaterReader.BuildBsonDocument(dataset);
            document.Remove("_id");
            document.Remove("header");

            Assert.AreEqual(datasetDocument, document);
        }

        private static void ValidateHeader(DicomFileMessage message, MessageHeader header, BsonDocument docHeader)
        {
            Assert.AreEqual(message.DicomFilePath, docHeader["DicomFilePath"].AsString);

            Assert.True(docHeader.TryGetElement("MessageHeader", out var element));

            var messageHeaderDoc = (BsonDocument)element.Value;
            Assert.NotNull(messageHeaderDoc);

            Assert.AreEqual(header.ProducerProcessID, messageHeaderDoc["ProducerProcessID"].AsInt32);
            Assert.AreEqual(header.ProducerExecutableName, messageHeaderDoc["ProducerExecutableName"].AsString);
            Assert.AreEqual(header.OriginalPublishTimestamp, messageHeaderDoc["OriginalPublishTimestamp"].AsInt64);

            Assert.True(messageHeaderDoc.TryGetElement("Parents", out element));

            string parentsString = element.Value.AsString;

            Assert.False(string.IsNullOrWhiteSpace(parentsString));
            Assert.AreEqual(Guid.NewGuid().ToString().Length, parentsString.Length);
        }

        [Test]
        public void TestErrorHandling()
        {
            _helper.Globals.MongoDbPopulatorOptions!.FailedWriteLimit = 1;

            var mockAdapter = new Mock<IMongoDbAdapter>();
            mockAdapter
                .Setup(x => x.WriteMany(It.IsAny<IList<BsonDocument>>(), It.IsAny<string>()))
                .Returns(WriteResult.Failure);

            var processor = new ImageMessageProcessor(_helper.Globals.MongoDbPopulatorOptions, mockAdapter.Object, 1, delegate { }) { Model = Mock.Of<IModel>() };

            Assert.Throws<ApplicationException>(() => processor.AddToWriteQueue(_helper.TestImageMessage, new MessageHeader(), 1));
        }

        /// <summary>
        /// Write a single image message and test the document format is as expected
        /// </summary>
        [Test]
        public void TestImageDocumentFormat()
        {
            GlobalOptions options = MongoDbPopulatorTestHelper.GetNewMongoDbPopulatorOptions();
            options.MongoDbPopulatorOptions!.MongoDbFlushTime = int.MaxValue / 1000;

            string collectionName = MongoDbPopulatorTestHelper.GetCollectionNameForTest("TestImageDocumentFormat");
            var testAdapter = new MongoDbAdapter("TestImageDocumentFormat", options.MongoDatabases!.DicomStoreOptions!, collectionName);

            var callbackUsed = false;
            Action<Exception> exceptionCallback = (exception) => { callbackUsed = true; };

            var processor = new ImageMessageProcessor(options.MongoDbPopulatorOptions, testAdapter, 1, exceptionCallback)
            {
                Model = Mock.Of<IModel>()
            };

            var header = new MessageHeader();

            // Max queue size set to 1 so will immediately process this
            processor.AddToWriteQueue(_helper.TestImageMessage, header, 1);

            Assert.False(callbackUsed);
            Assert.True(processor.AckCount == 1);

            IMongoCollection<BsonDocument> imageCollection = _helper.TestDatabase.GetCollection<BsonDocument>(collectionName + "_SR");

            Assert.True(imageCollection.CountDocuments(new BsonDocument()) == 1);

            BsonDocument doc = imageCollection.FindAsync(FilterDefinition<BsonDocument>.Empty).Result.Single();
            Validate(_helper.TestImageMessage, header, doc);
        }


        [Test]
        public void TestLargeMessageNack()
        {
            GlobalOptions options = MongoDbPopulatorTestHelper.GetNewMongoDbPopulatorOptions();
            options.MongoDbPopulatorOptions!.MongoDbFlushTime = int.MaxValue / 1000;

            var adapter = new MongoDbAdapter("ImageProcessor", options.MongoDatabases!.ExtractionStoreOptions!, "largeDocumentTest");
            var processor = new ImageMessageProcessor(options.MongoDbPopulatorOptions, adapter, 1, (Exception _) => { });
            var mockModel = Mock.Of<IModel>();
            processor.Model = mockModel;

            var dataset = new DicomDataset
            {
                new DicomUnlimitedText(DicomTag.SelectorUTValue,new string('x', 16*1024*1024))
            };

            string json = DicomTypeTranslater.SerializeDatasetToJson(dataset);

            var largeMessage = new DicomFileMessage
            {
                SeriesInstanceUID = "",
                StudyInstanceUID = "",
                SOPInstanceUID = "",
                DicomFilePath = "",
                DicomDataset = json
            };

            Assert.Throws<ApplicationException>(() => processor.AddToWriteQueue(largeMessage, new MessageHeader(), 1));

            dataset = new DicomDataset
            { 
                // Should be ok, getting close to the threshold
                new DicomUnlimitedText(DicomTag.SelectorUTValue,new string('x', 15*1024*1024 + 512))
            };

            json = DicomTypeTranslater.SerializeDatasetToJson(dataset);
            largeMessage.DicomDataset = json;

            processor.AddToWriteQueue(largeMessage, new MessageHeader(), 2);
            Assert.True(processor.AckCount == 1);
        }

        [Test]
        public void TestLargeDocumentSplitOk()
        {
            GlobalOptions options = MongoDbPopulatorTestHelper.GetNewMongoDbPopulatorOptions();
            options.MongoDbPopulatorOptions!.MongoDbFlushTime = int.MaxValue / 1000;

            var adapter = new MongoDbAdapter("ImageProcessor", options.MongoDatabases!.ExtractionStoreOptions!, "largeDocumentTest");
            var processor = new ImageMessageProcessor(options.MongoDbPopulatorOptions!, adapter, 2, (Exception e) => { });
            var mockModel = Mock.Of<IModel>();
            processor.Model = mockModel;

            var dataset = new DicomDataset
            {
                new DicomUnlimitedText(DicomTag.SelectorUTValue,new string('x', 15*1024*1024))
            };

            var largeMessage = new DicomFileMessage
            {
                SeriesInstanceUID = "",
                StudyInstanceUID = "",
                SOPInstanceUID = "",
                DicomFilePath = "",
                DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(dataset)
            };

            processor.AddToWriteQueue(largeMessage, new MessageHeader(), 1);
            processor.AddToWriteQueue(largeMessage, new MessageHeader(), 2);
        }
    }
}
