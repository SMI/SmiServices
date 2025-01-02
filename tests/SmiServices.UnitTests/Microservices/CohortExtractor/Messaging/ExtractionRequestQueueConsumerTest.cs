using Moq;
using NLog;
using NUnit.Framework;
using RabbitMQ.Client;
using SmiServices.Common.Events;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.Messaging;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortExtractor;
using SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using System.Threading;

namespace SmiServices.UnitTests.Microservices.CohortExtractor.Messaging;

public class ExtractionRequestQueueConsumerTest
{
    #region Fixture Methods

    private static readonly IFileSystem _fileSystem = new MockFileSystem();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    class FakeFulfiller : IExtractionRequestFulfiller
    {
        protected readonly Logger Logger;

        public List<IRejector> Rejectors { get; set; } = [];

        public Regex? ModalityRoutingRegex { get; set; }
        public Dictionary<ModalitySpecificRejectorOptions, IRejector> ModalitySpecificRejectors { get; set; }
            = [];

        public FakeFulfiller()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public IEnumerable<ExtractImageCollection> GetAllMatchingFiles(ExtractionRequestMessage message)
        {
            Logger.Debug($"Found {message.KeyTag}");

            foreach (var valueToLookup in message.ExtractionIdentifiers)
            {
                var results = new ExtractImageCollection(valueToLookup);
                var studyTagValue = "2";
                var seriesTagValue = "3";
                var instanceTagValue = "4";
                var rejection = false;
                var rejectionReason = "";
                var result = new QueryToExecuteResult(valueToLookup, studyTagValue, seriesTagValue, instanceTagValue, rejection, rejectionReason);
                if (!results.ContainsKey(result.SeriesTagValue!))
                    results.Add(result.SeriesTagValue!, []);
                results[result.SeriesTagValue!].Add(result);

                yield return results;
            }
        }
    }

    #endregion

    #region Test Methods

    [SetUp]
    public void SetUp()
    {
    }

    [TearDown]
    public void TearDown() { }

    #endregion

    #region Tests

    [Test]
    public void Test_ExtractionRequestQueueConsumer_AnonExtraction_RoutingKey()
    {
        GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Test_ExtractionRequestQueueConsumer_AnonExtraction_RoutingKey));
        globals.CohortExtractorOptions!.ExtractAnonRoutingKey = "anon";
        globals.CohortExtractorOptions.ExtractIdentRoutingKey = "";
        AssertMessagePublishedWithSpecifiedKey(globals, false, "anon");
    }

    [Test]
    public void Test_ExtractionRequestQueueConsumer_IdentExtraction_RoutingKey()
    {
        GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Test_ExtractionRequestQueueConsumer_IdentExtraction_RoutingKey));
        globals.CohortExtractorOptions!.ExtractAnonRoutingKey = "";
        globals.CohortExtractorOptions.ExtractIdentRoutingKey = "ident";
        AssertMessagePublishedWithSpecifiedKey(globals, true, "ident");
    }

    /// <summary>
    /// Checks that ExtractionRequestQueueConsumer publishes messages correctly according to the input message isIdentifiableExtraction value
    /// </summary>
    /// <param name="globals"></param>
    /// <param name="isIdentifiableExtraction"></param>
    /// <param name="expectedRoutingKey"></param>
    private void AssertMessagePublishedWithSpecifiedKey(GlobalOptions globals, bool isIdentifiableExtraction, string expectedRoutingKey)
    {
        var fakeFulfiller = new FakeFulfiller();

        var mockFileMessageProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
        string? fileMessageRoutingKey = null;
        mockFileMessageProducerModel
            .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), It.IsNotNull<string>()))
            .Callback((IMessage _, IMessageHeader __, string routingKey) => { fileMessageRoutingKey = routingKey; })
            .Returns(new MessageHeader());
        mockFileMessageProducerModel.Setup(x => x.WaitForConfirms());

        var mockFileInfoMessageProducerModel = new Mock<IProducerModel>(MockBehavior.Strict);
        mockFileInfoMessageProducerModel
            .Setup(x => x.SendMessage(It.IsAny<IMessage>(), It.IsAny<IMessageHeader>(), null))
            .Returns(new MessageHeader());
        mockFileInfoMessageProducerModel.Setup(x => x.WaitForConfirms());

        var msg = new ExtractionRequestMessage
        {
            JobSubmittedAt = DateTime.UtcNow,
            ExtractionJobIdentifier = Guid.NewGuid(),
            ProjectNumber = "1234",
            ExtractionDirectory = "1234/foo",
            IsIdentifiableExtraction = isIdentifiableExtraction,
            KeyTag = "foo",
            ExtractionIdentifiers = ["foo"],
            Modality = "CT",
        };

        var consumer = new ExtractionRequestQueueConsumer(
            globals.CohortExtractorOptions!,
            fakeFulfiller,
            new StudySeriesOriginalFilenameProjectPathResolver(_fileSystem),
            mockFileMessageProducerModel.Object,
            mockFileInfoMessageProducerModel.Object);

        var fatalCalled = false;
        FatalErrorEventArgs? fatalErrorEventArgs = null;
        consumer.OnFatal += (sender, args) =>
        {
            fatalCalled = true;
            fatalErrorEventArgs = args;
        };

        consumer.ProcessMessage(new MessageHeader(), msg, 1);

        Thread.Sleep(500); // Fatal call is race-y
        Assert.That(fatalCalled, Is.False, $"Fatal was called with {fatalErrorEventArgs}");
        Assert.That(consumer.AckCount, Is.EqualTo(1));
        Assert.That(fileMessageRoutingKey, Is.EqualTo(expectedRoutingKey));
    }

    #endregion
}
