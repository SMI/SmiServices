using Microservices.CohortPackager.Execution;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.ExtractJobStorage.MongoDB;
using Microservices.CohortPackager.Execution.JobProcessing.Notifying;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using MongoDB.Driver;
using NUnit.Framework;
using Smi.Common.Messages;
using Smi.Common.Messages.Extraction;
using Smi.Common.MessageSerialization;
using Smi.Common.MongoDB;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;


namespace Microservices.CohortPackager.Tests.Execution
{
    [TestFixture, RequiresMongoDb, RequiresRabbit]
    public class CohortPackagerHostTest
    {
        private const string ExtractRoot = "extractRoot";
        private string _projExtract1Dir;
        private string _projReportsDir;

        private readonly TestDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();


        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            string projExtractionsDir = Path.Combine("proj1", "extractions");
            _projExtract1Dir = Path.Combine(projExtractionsDir, "extract1");
            _projReportsDir = Path.Combine(projExtractionsDir, "reports");
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

        #region Tests

        private class TestLoggingNotifier : IJobCompleteNotifier
        {
            public bool JobCompleted { get; set; }

            public void NotifyJobCompleted(ExtractJobInfo jobInfo)
            {
                JobCompleted = true;
            }
        }

        [Test]
        public void HappyPath_AnonExtraction_SingleReport()
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
                ExtractionDirectory = _projExtract1Dir,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 2,
            };
            var testExtractFileCollectionInfoMessage1 = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1Dir,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-1-anon-1.dcm" },
                    // { new MessageHeader(), "series-1-anon-2.dcm" }, -- rejected
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
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
                IsIdentifiable = true,
                Report = failureReport,
                DicomFilePath = "series-2-orig-2.dcm",
            };


            GlobalOptions globals = new GlobalOptionsFactory().Load();
            globals.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 5;
            globals.CohortPackagerOptions.ReporterType = "FileReporter";
            globals.CohortPackagerOptions.ReportFormat = "Combined";

            MongoClient client = MongoClientHelpers.GetMongoClient(globals.MongoDatabases.ExtractionStoreOptions, "test", true);
            client.DropDatabase(globals.MongoDatabases.ExtractionStoreOptions.DatabaseName);

            using (var tester = new MicroserviceTester(
                globals.RabbitOptions,
                globals.CohortPackagerOptions.ExtractRequestInfoOptions,
                globals.CohortPackagerOptions.FileCollectionInfoOptions,
                globals.CohortPackagerOptions.NoVerifyStatusOptions,
                globals.CohortPackagerOptions.VerificationStatusOptions))
            {
                tester.SendMessage(globals.CohortPackagerOptions.ExtractRequestInfoOptions, new MessageHeader(), testExtractionRequestInfoMessage);
                tester.SendMessage(globals.CohortPackagerOptions.FileCollectionInfoOptions, new MessageHeader(), testExtractFileCollectionInfoMessage1);
                tester.SendMessage(globals.CohortPackagerOptions.FileCollectionInfoOptions, new MessageHeader(), testExtractFileCollectionInfoMessage2);
                tester.SendMessage(globals.CohortPackagerOptions.NoVerifyStatusOptions, new MessageHeader(), testExtractFileStatusMessage);
                tester.SendMessage(globals.CohortPackagerOptions.VerificationStatusOptions, new MessageHeader(), testIsIdentifiableMessage1);
                tester.SendMessage(globals.CohortPackagerOptions.VerificationStatusOptions, new MessageHeader(), testIsIdentifiableMessage2);

                globals.FileSystemOptions.ExtractRoot = ExtractRoot;
                var mockFileSystem = new MockFileSystem();
                var reportsDir = Path.Combine(ExtractRoot, _projReportsDir);
                mockFileSystem.Directory.CreateDirectory(reportsDir);
                mockFileSystem.Directory.CreateDirectory(Path.Combine(ExtractRoot, _projExtract1Dir));

                var notifier = new TestLoggingNotifier();
                var host = new CohortPackagerHost(
                    globals,
                    jobStore: null,
                    mockFileSystem,
                    notifier: notifier,
                    loadSmiLogConfig: false
                );
                host.Start();

                var timeoutSecs = 30;
                while (!notifier.JobCompleted && timeoutSecs > 0)
                {
                    --timeoutSecs;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                host.Stop("Test end");
                Assert.True(notifier.JobCompleted && timeoutSecs >= 0);

                string reportContent = mockFileSystem.File.ReadAllText(mockFileSystem.Path.Combine(reportsDir, "extract1_report.txt"));
                var expected = @$"
# SMI extraction validation report for testProj1/extract1

Job info:
-   Job submitted at:              {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}
-   Job extraction id:             {jobId}
-   Extraction tag:                SeriesInstanceUID
-   Extraction modality:           Unspecified
-   Requested identifier count:    2
-   Identifiable extraction:       No
-   Filtered extraction:           Yes

Report contents:

-   Verification failures
    -   Summary
    -   Full Details
-   Blocked files
-   Anonymisation failures

## Verification failures

### Summary

-   Tag: ScanOptions (1 total occurrence(s))
    -   Value: 'FOO' (1 occurrence(s))


### Full details

-   Tag: ScanOptions (1 total occurrence(s))
    -   Value: 'FOO' (1 occurrence(s))
        -   series-2-anon-2.dcm


## Blocked files

-   ID: series-1
    -   1x 'rejected - blah'

## Anonymisation failures

-   file 'series-2-anon-1.dcm': 'Couldn't anonymise'

--- end of report ---
";
                Assert.AreEqual(expected, reportContent);
            }
        }

        [Test]
        public void HappyPath_AnonExtraction_SplitReport()
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
                ExtractionDirectory = _projExtract1Dir,
                KeyTag = "SeriesInstanceUID",
                KeyValueCount = 2,
            };
            var testExtractFileCollectionInfoMessage1 = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1Dir,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "series-1-anon-1.dcm" },
                    // { new MessageHeader(), "series-1-anon-2.dcm" }, -- rejected
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
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
                IsIdentifiable = true,
                Report = failureReport,
                DicomFilePath = "series-2-orig-2.dcm",
            };


            GlobalOptions globals = new GlobalOptionsFactory().Load();
            globals.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 5;
            globals.CohortPackagerOptions.ReporterType = "FileReporter";
            globals.CohortPackagerOptions.ReportFormat = "Split";

            MongoClient client = MongoClientHelpers.GetMongoClient(globals.MongoDatabases.ExtractionStoreOptions, "test", true);
            client.DropDatabase(globals.MongoDatabases.ExtractionStoreOptions.DatabaseName);

            using (var tester = new MicroserviceTester(
                globals.RabbitOptions,
                globals.CohortPackagerOptions.ExtractRequestInfoOptions,
                globals.CohortPackagerOptions.FileCollectionInfoOptions,
                globals.CohortPackagerOptions.NoVerifyStatusOptions,
                globals.CohortPackagerOptions.VerificationStatusOptions))
            {
                tester.SendMessage(globals.CohortPackagerOptions.ExtractRequestInfoOptions, new MessageHeader(), testExtractionRequestInfoMessage);
                tester.SendMessage(globals.CohortPackagerOptions.FileCollectionInfoOptions, new MessageHeader(), testExtractFileCollectionInfoMessage1);
                tester.SendMessage(globals.CohortPackagerOptions.FileCollectionInfoOptions, new MessageHeader(), testExtractFileCollectionInfoMessage2);
                tester.SendMessage(globals.CohortPackagerOptions.NoVerifyStatusOptions, new MessageHeader(), testExtractFileStatusMessage);
                tester.SendMessage(globals.CohortPackagerOptions.VerificationStatusOptions, new MessageHeader(), testIsIdentifiableMessage1);
                tester.SendMessage(globals.CohortPackagerOptions.VerificationStatusOptions, new MessageHeader(), testIsIdentifiableMessage2);

                globals.FileSystemOptions.ExtractRoot = ExtractRoot;
                var mockFileSystem = new MockFileSystem();
                var fullreportsDir = Path.Combine(ExtractRoot, _projReportsDir);
                mockFileSystem.Directory.CreateDirectory(fullreportsDir);
                mockFileSystem.Directory.CreateDirectory(Path.Combine(ExtractRoot, _projExtract1Dir));

                var notifier = new TestLoggingNotifier();
                var host = new CohortPackagerHost(
                    globals,
                    jobStore: null,
                    mockFileSystem,
                    notifier: notifier,
                    loadSmiLogConfig: false
                );
                host.Start();

                var timeoutSecs = 30;
                while (!notifier.JobCompleted && timeoutSecs > 0)
                {
                    --timeoutSecs;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                host.Stop("Test end");
                Assert.True(notifier.JobCompleted && timeoutSecs >= 0);

                List<string> allFiles = mockFileSystem.AllFiles.ToList();
                Assert.AreEqual(7, allFiles.Count);

                string extractionName = _projExtract1Dir.Split('/', '\\')[^1];
                string extractionReportsDir = mockFileSystem.Path.Combine(ExtractRoot, _projReportsDir, extractionName);

                var expectedReadmeText =
$@"# SMI extraction validation report for testProj1/{extractionName}

Job info:
-   Job submitted at:              {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}
-   Job extraction id:             {jobId}
-   Extraction tag:                SeriesInstanceUID
-   Extraction modality:           Unspecified
-   Requested identifier count:    2
-   Identifiable extraction:       No
-   Filtered extraction:           Yes

Files included:
-   README.md (this file)
-   pixel_data_summary.csv
-   pixel_data_full.csv
-   pixel_data_word_length_frequencies.csv
-   tag_data_summary.csv
-   tag_data_full.csv

This file contents:
-   Blocked files
-   Anonymisation failures

## Blocked files

-   ID: series-1
    -   1x 'rejected - blah'

## Anonymisation failures

-   file 'series-2-anon-1.dcm': 'Couldn't anonymise'

--- end of report ---
";
                string readmeText = mockFileSystem.File.ReadAllText(mockFileSystem.Path.Combine(extractionReportsDir, "README.md"));
                Console.WriteLine(readmeText);
                Assert.AreEqual(expectedReadmeText, readmeText);

                // TODO(rkm 2020-10-29) Test other report content

                // debug
                foreach (var f in allFiles)
                {
                    Console.WriteLine($"--- {f.Split('/', '\\')[^1]} ---");
                    Console.WriteLine(mockFileSystem.File.ReadAllText(f));
                }
            }
        }

        [Test]
        public void Test_CohortPackagerHost_IdentifiableExtraction()
        {
            Guid jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1Dir,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 1,
                IsIdentifiableExtraction = true,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = _dateTimeProvider.UtcNow(),
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
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
                ExtractionDirectory = _projExtract1Dir,
                Status = ExtractedFileStatus.FileMissing,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-2.dcm",
                IsIdentifiableExtraction = true,
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();
            globals.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 5;
            globals.CohortPackagerOptions.ReportFormat = null;

            MongoClient client = MongoClientHelpers.GetMongoClient(globals.MongoDatabases.ExtractionStoreOptions, "test", true);
            client.DropDatabase(globals.MongoDatabases.ExtractionStoreOptions.DatabaseName);

            using var tester = new MicroserviceTester(
                globals.RabbitOptions,
                globals.CohortPackagerOptions.ExtractRequestInfoOptions,
                globals.CohortPackagerOptions.FileCollectionInfoOptions,
                globals.CohortPackagerOptions.NoVerifyStatusOptions,
                globals.CohortPackagerOptions.VerificationStatusOptions
            );

            tester.SendMessage(globals.CohortPackagerOptions.ExtractRequestInfoOptions, new MessageHeader(), testExtractionRequestInfoMessage);
            tester.SendMessage(globals.CohortPackagerOptions.FileCollectionInfoOptions, new MessageHeader(), testExtractFileCollectionInfoMessage);
            tester.SendMessage(globals.CohortPackagerOptions.NoVerifyStatusOptions, new MessageHeader(), testExtractFileStatusMessage1);
            tester.SendMessage(globals.CohortPackagerOptions.NoVerifyStatusOptions, new MessageHeader(), testExtractFileStatusMessage2);

            MongoDbOptions mongoDbOptions = globals.MongoDatabases.ExtractionStoreOptions;
            var jobStore = new MongoExtractJobStore(
                MongoClientHelpers.GetMongoClient(mongoDbOptions, "CohortPackager-Test"),
                mongoDbOptions.DatabaseName
            );

            globals.FileSystemOptions.ExtractRoot = ExtractRoot;
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.Directory.CreateDirectory(globals.FileSystemOptions.FileSystemRoot);
            var reportsDir = Path.Combine(ExtractRoot, _projReportsDir);
            mockFileSystem.Directory.CreateDirectory(reportsDir);
            mockFileSystem.Directory.CreateDirectory(Path.Combine(ExtractRoot, _projExtract1Dir));

            var reporter = new FileReporter(jobStore, mockFileSystem, ExtractRoot, ReportFormat.Combined);

            var notifier = new TestLoggingNotifier();
            var host = new CohortPackagerHost(
                globals,
                jobStore,
                reporter: reporter,
                notifier: notifier,
                loadSmiLogConfig: false
            );
            host.Start();

            var timeoutSecs = 30;
            while (!notifier.JobCompleted && timeoutSecs > 0)
            {
                --timeoutSecs;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            host.Stop("Test end");
            Assert.True(notifier.JobCompleted && timeoutSecs >= 0);

            List<string> allFiles = mockFileSystem.AllFiles.ToList();
            Assert.AreEqual(2, allFiles.Count);
            string reportContent = mockFileSystem.File.ReadAllText(mockFileSystem.Path.Combine(reportsDir, "extract1_report.txt"));
            var expected = $@"
# SMI extraction validation report for testProj1/extract1

Job info:
-   Job submitted at:              {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}
-   Job extraction id:             {jobId}
-   Extraction tag:                StudyInstanceUID
-   Extraction modality:           MR
-   Requested identifier count:    1
-   Identifiable extraction:       Yes
-   Filtered extraction:           Yes

Report contents:
-   Missing file list (files which were selected from an input ID but could not be found)

## Missing file list

-   study-1-orig-2.dcm

--- end of report ---
";
            Assert.AreEqual(expected, reportContent);
        }

        #endregion
    }
}
