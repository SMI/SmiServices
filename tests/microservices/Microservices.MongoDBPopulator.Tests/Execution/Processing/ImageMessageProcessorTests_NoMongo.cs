﻿
using Dicom;
using DicomTypeTranslation;
using Microservices.MongoDBPopulator.Execution;
using Microservices.MongoDBPopulator.Execution.Processing;
using MongoDB.Bson;
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
    [TestFixture]
    public class ImageMessageProcessorTests_NoMongo
    {
        private GlobalOptions _testOptions;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [SetUp]
        public void SetUp()
        {
            _testOptions = new GlobalOptionsFactory().Load();
        }

        /// <summary>
        /// Asserts that messages in the write queue are acknowledged even if an error occurs later in the modality batch
        /// </summary>
        [Test]
        public void ImageProcessor_FailInModalityBatch_AcksWrittenDocuments()
        {
            _testOptions.MongoDbPopulatorOptions.FailedWriteLimit = 1;
            _testOptions.MongoDbPopulatorOptions.MongoDbFlushTime = int.MaxValue / 1000;

            var testModalities = new[] { "MR", "MR", "MR", "SR", "SR" };

            var testAdapter = new MongoTestAdapter();
            var processor = new ImageMessageProcessor(_testOptions.MongoDbPopulatorOptions, testAdapter, testModalities.Length + 1, null);

            var mockModel = new Mock<IModel>();
            mockModel.Setup(x => x.BasicAck(It.Is<ulong>(y => y == ulong.MaxValue), It.IsAny<bool>()))
                .Callback(() => throw new Exception("BasicAck called with delivery tag for CT message"));

            processor.Model = mockModel.Object;

            var ds = new DicomDataset();
            var msg = new DicomFileMessage
            {
                DicomFilePath = "",
                NationalPACSAccessionNumber = ""
            };

            for (var i = 0; i < testModalities.Length; ++i)
            {
                string modality = testModalities[i];
                ds.AddOrUpdate(DicomTag.Modality, modality);
                msg.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);
                processor.AddToWriteQueue(msg, null, (ulong)i);
            }

            ds.AddOrUpdate(DicomTag.Modality, "CT");
            msg.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);

            Assert.Throws<ApplicationException>(() => processor.AddToWriteQueue(msg, new MessageHeader(), ulong.MaxValue));
            Assert.AreEqual(5, processor.AckCount);
        }
    }

    public class MongoTestAdapter : IMongoDbAdapter
    {
        public WriteResult WriteMany(IList<BsonDocument> toWrite, string collectionNamePostfix = null)
        {
            Assert.NotZero(toWrite.Count);

            BsonDocument doc = toWrite.First();
            Assert.True(toWrite.All(x => x["Modality"] == doc["Modality"]));

            // Fails for "CT" modalities
            switch (doc["Modality"].AsString)
            {
                case "MR":
                    return WriteResult.Success;
                case "CT":
                    return WriteResult.Failure;
                case "SR":
                    return WriteResult.Success;
                default:
                    Assert.Fail($"No case for {doc["Modality"]}");
                    return WriteResult.Unknown;
            }
        }
    }
}
