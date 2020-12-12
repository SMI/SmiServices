using Microservices.CohortPackager.Execution;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using MongoDB.Driver;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;


namespace Microservices.CohortPackager.Tests.Execution
{
    [TestFixture, RequiresMongoDb, RequiresRabbit]
    public class CohortPackagerHostTest
    {
        private string _testDirAbsolute;
        private string _extractRootAbsolute;
        private string _projExtractionsDirRelative;
        private string _projExtract1DirRelative;
        private string _projExtract1DirAbsolute;
        private string _projReportsDirAbsolute;

        private readonly TestDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _projExtractionsDirRelative = Path.Combine("proj1", "extractions");
            _projExtract1DirRelative = Path.Combine(_projExtractionsDirRelative, "extract1");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp()
        {
            _testDirAbsolute = Path.Combine(Path.GetTempPath(), "nunit-smiservices", $"{TestContext.CurrentContext.Test.FullName}-{Guid.NewGuid().ToString().Split('-')[0]}");
            _extractRootAbsolute = Path.Combine(_testDirAbsolute, "extractRoot");
            _projExtract1DirAbsolute = Path.Combine(_extractRootAbsolute, _projExtract1DirRelative);
            _projReportsDirAbsolute = Path.Combine(_extractRootAbsolute, _projExtractionsDirRelative, "reports");

            // NOTE(rkm 2020-11-19) This would normally be created by one of the other services
            Directory.CreateDirectory(_projExtract1DirAbsolute);
        }

        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome == ResultState.Failure)
                return;

            Directory.Delete(_testDirAbsolute, recursive: true);
        }

        private bool HaveFiles() => Directory.Exists(_projReportsDirAbsolute) && Directory.EnumerateFiles(_projExtract1DirAbsolute).Any();

        private void VerifyReports(GlobalOptions globals, ReportFormat reportFormat, IEnumerable<Tuple<ConsumerOptions, IMessage>> toSend)
        {
            globals.FileSystemOptions.ExtractRoot = _extractRootAbsolute;
            globals.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 5;
            globals.CohortPackagerOptions.ReporterType = "FileReporter";
            globals.CohortPackagerOptions.ReportFormat = reportFormat.ToString();

            MongoClient client = MongoClientHelpers.GetMongoClient(globals.MongoDatabases.ExtractionStoreOptions, "test", true);
            client.DropDatabase(globals.MongoDatabases.ExtractionStoreOptions.DatabaseName);

            using (var tester = new MicroserviceTester(
                globals.RabbitOptions,
                globals.CohortPackagerOptions.ExtractRequestInfoOptions,
                globals.CohortPackagerOptions.FileCollectionInfoOptions,
                globals.CohortPackagerOptions.NoVerifyStatusOptions,
                globals.CohortPackagerOptions.VerificationStatusOptions))
            {
                foreach ((ConsumerOptions consumerOptions, IMessage message) in toSend)
                    tester.SendMessage(consumerOptions, new MessageHeader(), message);

                var host = new CohortPackagerHost(
                    globals,
                    loadSmiLogConfig: false
                );

                host.Start();

                var timeoutSecs = 10;

                while (!HaveFiles() && timeoutSecs > 0)
                {
                    --timeoutSecs;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                host.Stop("Test end");
            }
            
            const string firstLine = "# SMI extraction validation report for testProj1/extract1";
            switch (reportFormat)
            {
                case ReportFormat.Combined:
                    {
                        string reportContent = File.ReadAllText(Path.Combine(_projReportsDirAbsolute, "extract1_report.txt"));
                        Assert.True(reportContent.StartsWith(firstLine));
                        break;
                    }
                case ReportFormat.Split:
                    {
                        string extract1ReportsDirAbsolute = Path.Combine(_projReportsDirAbsolute, "extract1");
                        Assert.AreEqual(6, Directory.GetFiles(extract1ReportsDirAbsolute).Length);
                        string reportContent = File.ReadAllText(Path.Combine(extract1ReportsDirAbsolute, "README.md"));
                        Assert.True(reportContent.StartsWith(firstLine));
                        break;
                    }
                default:
                    Assert.Fail($"No case for ReportFormat {reportFormat}");
                    break;
            }
        }

        #endregion

        #region Tests

        [TestCase(ReportFormat.Combined)]
        [TestCase(ReportFormat.Split)]
        public void Integration_CombinedReport_HappyPath(ReportFormat reportFormat)
        {
            // Test messages:
            //  - series-1
            //      - series-1-anon-1.dcm -> valid

            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 1,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
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
                ExtractionDirectory = _projExtract1DirRelative,
                IsIdentifiable = false,
                Report = "[]",
                DicomFilePath = "series-1-orig-1.dcm",
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();

            VerifyReports(
                globals,
                reportFormat,
                new[]
                {
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.ExtractRequestInfoOptions, testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions, testExtractFileCollectionInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions, testIsIdentifiableMessage),
                }
            );
        }

        [TestCase(ReportFormat.Combined)]
        [TestCase(ReportFormat.Split)]
        public void Integration_CombinedReport_BumpyRoad(ReportFormat reportFormat)
        {
            // Test messages:
            //  - series-1
            //      - series-1-anon-1.dcm -> valid
            //      - series-1-anon-2.dcm -> rejected
            //  - series-2
            //      - series-2-anon-1.dcm -> fails anonymisation
            //      - series-2-anon-2.dcm -> fails validation

            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 2,
            };
            var testExtractFileCollectionInfoMessage1 = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
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
                ExtractionDirectory = _projExtract1DirRelative,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-2-anon-1.dcm" },
                    { new MessageHeader(), "series-2-anon-2.dcm" },
                },
                RejectionReasons = new Dictionary<string, int>(),
                KeyValue = "series-2",
            };
            var testExtractFileStatusMessage = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "series-2-anon-1.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
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
                ExtractionDirectory = _projExtract1DirRelative,
                IsIdentifiable = false,
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
                ExtractionDirectory = _projExtract1DirRelative,
                IsIdentifiable = true,
                Report = failureReport,
                DicomFilePath = "series-2-orig-2.dcm",
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();

            VerifyReports(
                globals,
                reportFormat,
                new[]
                {
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.ExtractRequestInfoOptions, testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions,testExtractFileCollectionInfoMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions,  testExtractFileCollectionInfoMessage2),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions,  testExtractFileStatusMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions, testIsIdentifiableMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.VerificationStatusOptions, testIsIdentifiableMessage2),
                }
            );
        }

        [Test]
        public void Integration_IdentifiableExtraction_HappyPath()
        {
            var jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 1,
                IsIdentifiableExtraction = true,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
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
                ExtractionDirectory = _projExtract1DirRelative,
                Status = ExtractedFileStatus.Copied,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-1.dcm",
                IsIdentifiableExtraction = true,
            };
            var testExtractFileStatusMessage2 = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                OutputFilePath = "src_missing.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1DirRelative,
                Status = ExtractedFileStatus.FileMissing,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-2.dcm",
                IsIdentifiableExtraction = true,
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();

            VerifyReports(
                globals,
                ReportFormat.Combined,
                new[]
                {
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.ExtractRequestInfoOptions,  testExtractionRequestInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.FileCollectionInfoOptions,testExtractFileCollectionInfoMessage),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions,testExtractFileStatusMessage1),
                    new Tuple<ConsumerOptions, IMessage>(globals.CohortPackagerOptions.NoVerifyStatusOptions,  testExtractFileStatusMessage2),
                }
            );
        }

        #endregion
    }
}
