
using DicomTypeTranslation;
using FellowOakDicom;
using MongoDB.Bson;
using NUnit.Framework;
using SmiServices.Common.Messages;
using SmiServices.Common.Options;
using SmiServices.Microservices.MongoDBPopulator;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.Microservices.MongoDbPopulator;
using System;
using System.Diagnostics;
using System.Threading;


namespace SmiServices.IntegrationTests.Microservices.MongoDBPopulator;

[RequiresMongoDb, RequiresRabbit]
public class MongoDbPopulatorHostTests
{
    private MongoDbPopulatorTestHelper _helper = null!;

    [SetUp]
    public void SetUp()
    {
        _helper = new MongoDbPopulatorTestHelper();
        _helper.SetupSuite();
    }

    [TearDown]
    public void TearDown()
    {
        _helper.Dispose();
    }


    /// <summary>
    /// Asserts that we throw an exception if we can't connect to MongoDb on startup
    /// </summary>
    [Test]
    public void TestMissingMongoConnectionOnStartup()
    {
        GlobalOptions options = MongoDbPopulatorTestHelper.GetNewMongoDbPopulatorOptions();
        options.MongoDatabases!.DicomStoreOptions!.Port = 12345;

        // ReSharper disable once ObjectCreationAsStatement
        Assert.Throws<ArgumentException>(() => new MongoDbPopulatorMessageConsumer<DicomFileMessage>(options.MongoDatabases.DicomStoreOptions, options.MongoDbPopulatorOptions!, new ConsumerOptions()));
    }

    /// <summary>
    /// Tests basic operation of the populator by asserting the correct number of messages are written before a timeout
    /// </summary>
    /// <param name="nMessages"></param>
    [Test]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(100)]
    public void TestPopulatorBasic(int nMessages)
    {
        // Arrange

        string currentCollectionName = MongoDbPopulatorTestHelper.GetCollectionNameForTest(
            $"TestPopulatorBasic({nMessages})");

        _helper.Globals.MongoDbPopulatorOptions!.SeriesCollection = currentCollectionName;

        var tester = new MicroserviceTester(_helper.Globals.RabbitOptions!, _helper.Globals.MongoDbPopulatorOptions.SeriesQueueConsumerOptions!, _helper.Globals.MongoDbPopulatorOptions.ImageQueueConsumerOptions!);
        var host = new MongoDbPopulatorHost(_helper.Globals);

        host.Start();

        using var timeline = new TestTimeline(tester);
        var ds = new DicomDataset
        {
            new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3.4")
        };

        var message = new SeriesMessage
        {
            DirectoryPath = "DirectoryPath-test",
            StudyInstanceUID = "StudyInstanceUID-test",
            SeriesInstanceUID = "SeriesInstanceUID-test",
            ImagesInSeries = 123,
            DicomDataset = DicomTypeTranslater.SerializeDatasetToJson(ds)
        };

        // Act

        for (var i = 0; i < nMessages; i++)
            timeline.SendMessage(_helper.Globals.MongoDbPopulatorOptions.SeriesQueueConsumerOptions!, message);

        timeline.StartTimeline();

        var timeout = 30000;
        const int stepSize = 500;

        if (Debugger.IsAttached)
            timeout = int.MaxValue;

        var nWritten = 0L;

        while (nWritten < nMessages && timeout > 0)
        {
            nWritten = _helper.TestDatabase.GetCollection<BsonDocument>(currentCollectionName).CountDocuments(new BsonDocument());

            Thread.Sleep(stepSize);
            timeout -= stepSize;
        }

        // Assert

        if (timeout <= 0)
            Assert.Fail("Failed to process expected number of messages within the timeout");

        host.Stop("Test end");
        tester.Shutdown();
    }
}
