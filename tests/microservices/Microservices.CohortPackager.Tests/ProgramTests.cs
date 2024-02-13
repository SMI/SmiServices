using Microservices.CohortPackager.Execution;
using MongoDB.Driver;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microservices.CohortPackager.Tests;

[RequiresMongoDb, RequiresRabbit]
internal class ProgramTests
{
    #region Fixture Methods

    private readonly TestDateTimeProvider _dateTimeProvider = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestLogger.Setup();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    #endregion

    #region Test Methods

    [SetUp]
    public void SetUp()
    {
        SmiCliInit.InitSmiLogging = false;
    }

    [TearDown]
    public void TearDown() { }

    private static bool HaveFiles(PathFixtures pf) => Directory.Exists(pf.ProjReportsDirAbsolute) && Directory.EnumerateFiles(pf.ProjExtractDirAbsolute).Any();

    #endregion

    #region Tests

    [Test]
    public void RecreateReports_HappyPath()
    {
        // Arrange

        using var pf = new PathFixtures(nameof(RecreateReports_HappyPath));

        GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(RecreateReports_HappyPath));
        globals.FileSystemOptions!.ExtractRoot = pf.ExtractRootAbsolute;
        globals.CohortPackagerOptions!.JobWatcherTimeoutInSeconds = 5;
        globals.CohortPackagerOptions.VerificationMessageQueueProcessBatches = false;

        var jobId = Guid.NewGuid();
        var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
        {
            JobSubmittedAt = _dateTimeProvider.UtcNow(),
            ProjectNumber = "testProj1",
            ExtractionJobIdentifier = jobId,
            ExtractionDirectory = pf.ProjExtractDirRelative,
            KeyTag = "SeriesInstanceUID",
            KeyValueCount = 1,
            UserName = "testUser",
        };
        var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
        {
            JobSubmittedAt = _dateTimeProvider.UtcNow(),
            ProjectNumber = "testProj1",
            ExtractionJobIdentifier = jobId,
            ExtractionDirectory = pf.ProjExtractDirRelative,
            ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-1-anon-1.dcm" },
                },
            RejectionReasons = new Dictionary<string, int>
                {
                {"rejected - blah", 1 },
                },
            KeyValue = "series-1",
        };
        var testIsIdentifiableMessage = new ExtractedFileVerificationMessage
        {
            JobSubmittedAt = _dateTimeProvider.UtcNow(),
            OutputFilePath = "series-1-anon-1.dcm",
            ProjectNumber = "testProj1",
            ExtractionJobIdentifier = jobId,
            ExtractionDirectory = pf.ProjExtractDirRelative,
            Status = VerifiedFileStatus.NotIdentifiable,
            Report = "[]",
            DicomFilePath = "series-1-orig-1.dcm",
        };

        var toSend = new List<Tuple<ConsumerOptions, IMessage>>
        {
            new(globals.CohortPackagerOptions!.ExtractRequestInfoOptions!, testExtractionRequestInfoMessage),
            new(globals.CohortPackagerOptions.FileCollectionInfoOptions!, testExtractFileCollectionInfoMessage),
            new(globals.CohortPackagerOptions.VerificationStatusOptions!, testIsIdentifiableMessage),
        };

        using (var tester = new MicroserviceTester(
            globals.RabbitOptions!,
            globals.CohortPackagerOptions.ExtractRequestInfoOptions!,
            globals.CohortPackagerOptions.FileCollectionInfoOptions!,
            globals.CohortPackagerOptions.NoVerifyStatusOptions!,
            globals.CohortPackagerOptions.VerificationStatusOptions!))
        {
            foreach ((ConsumerOptions consumerOptions, IMessage message) in toSend)
                tester.SendMessage(consumerOptions, new MessageHeader(), message);

            var host = new CohortPackagerHost(globals);

            host.Start();

            var timeoutSecs = 10;

            while (!HaveFiles(pf) && timeoutSecs > 0)
            {
                --timeoutSecs;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            host.Stop("Test end");
        }

        var args = new[] { "-r", jobId.ToString(), };

        // Act

        var rc = Program.Main(args);

        // Assert

        Assert.AreEqual(0, rc);
    }

    [Test]
    public void RecreateReports_MissingJob_ReturnsNonZero()
    {
        // Arrange

        var args = new[] { "-r", Guid.NewGuid().ToString(), };

        // Act

        var rc = Program.Main(args);

        // Assert

        Assert.AreEqual(1, rc);
    }

    [Test]
    public void RecreateReports_MissingExtractionStoreOptions_ReturnsNonZero()
    {
        // Arrange

        var optionsContent = @"
LoggingOptions:
    LogConfigFile:
";

        File.WriteAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "empty.yml"), optionsContent);

        var args = new[] { "-r", Guid.NewGuid().ToString(), "-y", "empty.yml" };

        // Act

        var rc = Program.Main(args);

        // Assert

        Assert.AreEqual(1, rc);
    }

    [Test]
    public void RecreateReports_MissingDatabaseName_ReturnsNonZero()
    {
        // Arrange

        var optionsContent = @"
LoggingOptions:
    LogConfigFile:
MongoDatabases:  
  ExtractionStoreOptions:
    DatabaseName:
";

        File.WriteAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "empty.yml"), optionsContent);

        var args = new[] { "-r", Guid.NewGuid().ToString(), "-y", "empty.yml" };

        // Act

        var rc = Program.Main(args);

        // Assert

        Assert.AreEqual(1, rc);
    }

    #endregion
}
