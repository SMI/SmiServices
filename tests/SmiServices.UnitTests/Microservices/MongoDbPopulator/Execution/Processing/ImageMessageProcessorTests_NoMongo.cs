
using DicomTypeTranslation;
using FellowOakDicom;
using MongoDB.Bson;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using SmiServices.Microservices.MongoDBPopulator;
using SmiServices.Microservices.MongoDBPopulator.Processing;
using System;
using System.Collections.Generic;
using System.Linq;


namespace SmiServices.UnitTests.Microservices.MongoDbPopulator.Execution.Processing;

public class ImageMessageProcessorTests_NoMongo
{
    private GlobalOptions _testOptions = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [SetUp]
    public void SetUp()
    {
        _testOptions = new GlobalOptionsFactory().Load(nameof(ImageMessageProcessorTests_NoMongo));
    }

    /// <summary>
    /// Asserts that messages in the write queue are acknowledged even if an error occurs later in the modality batch
    /// </summary>
    [Test]
    public void ImageProcessor_FailInModalityBatch_AcksWrittenDocuments()
    {
        _testOptions.MongoDbPopulatorOptions!.FailedWriteLimit = 1;
        _testOptions.MongoDbPopulatorOptions.MongoDbFlushTime = int.MaxValue / 1000;

        var testModalities = new[] { "MR", "MR", "MR", "SR", "SR" };

        var testAdapter = new MongoTestAdapter();
        var processor = new ImageMessageProcessor(_testOptions.MongoDbPopulatorOptions, testAdapter, testModalities.Length + 1, null!);

        var ds = new DicomDataset();
        var msg = new DicomFileMessage
        {
            DicomFilePath = "",
        };

        for (var i = 0; i < testModalities.Length; ++i)
        {
            string modality = testModalities[i];
            ds.AddOrUpdate(DicomTag.Modality, modality);
            msg.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);
            processor.AddToWriteQueue(msg, new MessageHeader(), (ulong)i);
        }

        ds.AddOrUpdate(DicomTag.Modality, "CT");
        msg.DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds);

        Assert.Throws<ApplicationException>(() => processor.AddToWriteQueue(msg, new MessageHeader(), ulong.MaxValue));
        Assert.That(processor.AckCount, Is.EqualTo(5));
    }
}

public class MongoTestAdapter : IMongoDbAdapter
{
    public WriteResult WriteMany(IList<BsonDocument> toWrite, string? collectionNamePostfix = null)
    {
        Assert.That(toWrite, Is.Not.Empty);

        BsonDocument doc = toWrite.First();
        Assert.That(toWrite.All(x => x["Modality"] == doc["Modality"]), Is.True);

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
