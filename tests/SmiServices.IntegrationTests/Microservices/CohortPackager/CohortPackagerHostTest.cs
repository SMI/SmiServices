using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using SmiServices.Common;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messages;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Common.MessageSerialization;
using SmiServices.Common.MongoDB;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortPackager;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.UnitTests.Common;
using SmiServices.UnitTests.Microservices.CohortPackager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;


namespace SmiServices.IntegrationTests.Microservices.CohortPackager
{
    [TestFixture, RequiresMongoDb, RequiresRabbit]
    public class CohortPackagerHostTest
    {
        private readonly TestDateTimeProvider _dateTimeProvider = new();

        #region Fixture Methods

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Fixtures

        #endregion

        #region Tests

        private static bool HaveFiles(PathFixtures pf) => Directory.Exists(pf.ProjReportsDirAbsolute) && Directory.EnumerateFiles(pf.ProjExtractDirAbsolute).Any();

        private static void VerifyReports(GlobalOptions globals, PathFixtures pf, IEnumerable<Tuple<ConsumerOptions, IMessage>> toSend, bool isIdentifiableExtraction)
        {
            globals.FileSystemOptions!.ExtractRoot = pf.ExtractRootAbsolute;
            globals.CohortPackagerOptions!.JobWatcherTimeoutInSeconds = 5;
            globals.CohortPackagerOptions.VerificationMessageQueueProcessBatches = false;

            MongoClient client = MongoClientHelpers.GetMongoClient(globals.MongoDatabases!.ExtractionStoreOptions!, "test", true);
            globals.MongoDatabases.ExtractionStoreOptions!.DatabaseName += "-" + Guid.NewGuid().ToString().Split('-')[0];
            client.DropDatabase(globals.MongoDatabases.ExtractionStoreOptions.DatabaseName);

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

            var firstLine = $"# SMI extraction validation report for testProj1 {pf.ExtractName}{globals.CohortPackagerOptions.ReportNewLine}";

            string extractReportsDirAbsolute = Path.Combine(pf.ProjReportsDirAbsolute, pf.ExtractName);
            var expectedReports = isIdentifiableExtraction ? 3 : 4;
            Assert.That(Directory.GetFiles(extractReportsDirAbsolute), Has.Length.EqualTo(expectedReports));
            string[] reportContent = File.ReadAllLines(Path.Combine(extractReportsDirAbsolute, "README.md"));
            Assert.That(reportContent[0], Is.EqualTo(firstLine));
        }

        [Test]
        public void Integration_HappyPath()
        {
            // Test messages:
            //  - series-1
            //      - series-1-anon-1.dcm -> valid

            using var pf = new PathFixtures(nameof(Integration_HappyPath));

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

            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Integration_HappyPath));

            VerifyReports(
                globals,
                pf,
                [
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions!.ExtractRequestInfoOptions!, testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions!, testExtractFileCollectionInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions!, testIsIdentifiableMessage),
                ],
                isIdentifiableExtraction: false
            );
        }

        [Test]
        public void Integration_BumpyRoad()
        {
            // Test messages:
            //  - series-1
            //      - series-1-anon-1.dcm -> valid
            //      - series-1-anon-2.dcm -> rejected
            //  - series-2
            //      - series-2-anon-1.dcm -> fails anonymisation
            //      - series-2-anon-2.dcm -> fails validation

            using var pf = new PathFixtures(nameof(Integration_BumpyRoad));

            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 2,
                UserName = "testUser",
            };
            var testExtractFileCollectionInfoMessage1 = new ExtractFileCollectionInfoMessage
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
            var testExtractFileCollectionInfoMessage2 = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-2-anon-1.dcm" },
                    { new MessageHeader(), "series-2-anon-2.dcm" },
                },
                RejectionReasons = [],
                KeyValue = "series-2",
            };
            var testExtractFileStatusMessage = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "series-2-anon-1.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                Status = ExtractedFileStatus.ErrorWontRetry,
                StatusMessage = "Couldn't anonymise",
                DicomFilePath = "series-2-orig-1.dcm",
            };
            var testIsIdentifiableMessage1 = new ExtractedFileVerificationMessage
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
            const string failureReport = @"
[
    {
        'Parts': [],
        'Resource': 'series-2-anon-2.dcm',
        'ResourcePrimaryKey': '1.2.3.4',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'FOO'
    }
]";
            var testIsIdentifiableMessage2 = new ExtractedFileVerificationMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "series-2-anon-2.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                Status = VerifiedFileStatus.IsIdentifiable,
                Report = failureReport,
                DicomFilePath = "series-2-orig-2.dcm",
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Integration_BumpyRoad));

            VerifyReports(
                globals,
                pf,
                [
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions!.ExtractRequestInfoOptions!, testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions!,testExtractFileCollectionInfoMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions!,  testExtractFileCollectionInfoMessage2),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions!,  testExtractFileStatusMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions!, testIsIdentifiableMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions!, testIsIdentifiableMessage2),
                ],
                isIdentifiableExtraction: false
            );
        }

        [Test]
        public void Integration_IdentifiableExtraction_HappyPath()
        {
            using var pf = new PathFixtures(nameof(Integration_IdentifiableExtraction_HappyPath));

            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 1,
                UserName = "testUser",
                IsIdentifiableExtraction = true,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "out1.dcm" },
                    { new MessageHeader(), "out2.dcm" },
                },
                RejectionReasons = new Dictionary<string, int>
                {
                    {"rejected - blah", 1 },
                },
                KeyValue = "study-1",
                IsIdentifiableExtraction = true,
            };
            var testExtractFileStatusMessage1 = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "src.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                Status = ExtractedFileStatus.Copied,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-1.dcm",
                IsIdentifiableExtraction = true,
            };
            var testExtractFileStatusMessage2 = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = null,
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = pf.ProjExtractDirRelative,
                Status = ExtractedFileStatus.FileMissing,
                StatusMessage = "Couldn't find src_missing.dcm",
                DicomFilePath = "src_missing.dcm",
                IsIdentifiableExtraction = true,
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(Integration_IdentifiableExtraction_HappyPath));

            VerifyReports(
                globals,
                pf,
                [
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions!.ExtractRequestInfoOptions!,  testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions!,testExtractFileCollectionInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions!,testExtractFileStatusMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions!,  testExtractFileStatusMessage2),
                ],
                isIdentifiableExtraction: true
            );
        }

        [Test]
        public void Constructor_JobStoreDateProvider_ThrowsException()
        {
            // Arrange

            var globals = new GlobalOptionsFactory().Load(nameof(Constructor_JobStoreDateProvider_ThrowsException));

            // Act

            CohortPackagerHost constructor() => new(globals, new Mock<IExtractJobStore>().Object, null, null, null, new Mock<IMessageBroker>().Object, new DateTimeProvider());

            // Assert

            var exc = Assert.Throws<ArgumentException>(() => constructor());
            Assert.That(exc!.Message, Is.EqualTo("jobStore and dateTimeProvider are mutually exclusive arguments"));
        }

        [Test]
        public void Constructor_InvalidExtractRoot_ThrowsException()
        {
            // Arrange

            var globals = new GlobalOptionsFactory().Load(nameof(Constructor_InvalidExtractRoot_ThrowsException));
            globals.FileSystemOptions!.ExtractRoot = "  ";

            // Act

            CohortPackagerHost constructor() => new(globals, new Mock<IExtractJobStore>().Object, null, null, null, new Mock<IMessageBroker>().Object, null);

            // Assert

            var exc = Assert.Throws<ArgumentOutOfRangeException>(() => constructor());
            Assert.That(exc!.Message, Is.EqualTo("Specified argument was out of the range of valid values. (Parameter 'globals')"));
        }

        #endregion
    }
}
