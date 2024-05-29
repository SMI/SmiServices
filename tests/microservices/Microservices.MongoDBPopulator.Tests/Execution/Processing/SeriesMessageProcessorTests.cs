
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
    public class SeriesMessageProcessorTests
    {
        private MongoDbPopulatorTestHelper _helper = null!;

        private readonly List<string> _seriesMessageProps = typeof(SeriesMessage).GetProperties().Select(x => x.Name).ToList();

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

        private void Validate(SeriesMessage message, BsonDocument document)
        {
            Assert.NotNull(message);
            Assert.NotNull(document);

            Assert.True(document.TryGetElement("header", out var element));

            var docHeader = (BsonDocument)element.Value;
            Assert.That(docHeader.ElementCount,Is.EqualTo(_seriesMessageProps.Count - 3));
            Assert.That(docHeader["DirectoryPath"].AsString,Is.EqualTo(message.DirectoryPath));
            Assert.That(docHeader["ImagesInSeries"].AsInt32,Is.EqualTo(message.ImagesInSeries));

            DicomDataset dataset = DicomTypeTranslater.DeserializeJsonToDataset(message.DicomDataset);
            Assert.NotNull(dataset);

            BsonDocument datasetDocument = DicomTypeTranslaterReader.BuildBsonDocument(dataset);
            document.Remove("_id");
            document.Remove("header");

            Assert.That(document,Is.EqualTo(datasetDocument));
        }

        [Test]
        public void TestErrorHandling()
        {
            _helper.Globals.MongoDbPopulatorOptions!.FailedWriteLimit = 1;

            var mockAdapter = new Mock<IMongoDbAdapter>();
            mockAdapter
                .Setup(x => x.WriteMany(It.IsAny<IList<BsonDocument>>(), It.IsAny<string>()))
                .Returns(WriteResult.Failure);

            var processor = new SeriesMessageProcessor(_helper.Globals.MongoDbPopulatorOptions, mockAdapter.Object, 1, delegate { }) { Model = Mock.Of<IModel>() };

            Assert.Throws<ApplicationException>(() => processor.AddToWriteQueue(_helper.TestSeriesMessage, new MessageHeader(), 1));
        }

        /// <summary>
        /// Write a single series message and test the document format is as expected
        /// </summary>
        [Test]
        public void TestSeriesDocumentFormat()
        {
            GlobalOptions options = MongoDbPopulatorTestHelper.GetNewMongoDbPopulatorOptions();
            options.MongoDbPopulatorOptions!.MongoDbFlushTime = int.MaxValue;

            string collectionName = MongoDbPopulatorTestHelper.GetCollectionNameForTest("TestSeriesDocumentFormat");
            var testAdapter = new MongoDbAdapter("TestSeriesDocumentFormat", options.MongoDatabases!.DicomStoreOptions!, collectionName);

            var callbackUsed = false;
            Action<Exception> exceptionCallback = (exception) => { callbackUsed = true; };

            var processor = new SeriesMessageProcessor(options.MongoDbPopulatorOptions, testAdapter, 1, exceptionCallback)
            {
                Model = Mock.Of<IModel>()
            };

            // Max queue size set to 1 so will immediately process this
            processor.AddToWriteQueue(_helper.TestSeriesMessage, new MessageHeader(), 1);

            Assert.False(callbackUsed);
            Assert.True(processor.AckCount == 1);

            IMongoCollection<BsonDocument> collection = _helper.TestDatabase.GetCollection<BsonDocument>(collectionName);

            Assert.True(collection.CountDocuments(new BsonDocument()) == 1);

            BsonDocument document = collection.Find(_ => true).ToList()[0];

            Validate(_helper.TestSeriesMessage, document);
        }
    }
}
