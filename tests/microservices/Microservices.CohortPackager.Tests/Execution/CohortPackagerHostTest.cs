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
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;


namespace Microservices.CohortPackager.Tests.Execution
{
    [TestFixture, RequiresMongoDb, RequiresRabbit]
    public class CohortPackagerHostTest
    {
        private const string ExtractRoot = "extractRoot";
        private string _projectExtractDir;
        private string _extractionDir;

        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _projectExtractDir = Path.Combine("proj1", "extractions");
            _extractionDir = Path.Combine(_projectExtractDir, "extract1");
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

        private class TestReporter : IJobReporter
        {
            public bool ReportCreated;
            public void CreateReport(Guid jobId) => ReportCreated = true;
        }

        private class TestLoggingNotifier : IJobCompleteNotifier
        {
            public bool JobCompleted { get; set; }

            public void NotifyJobCompleted(ExtractJobInfo jobInfo)
            {
                JobCompleted = true;
            }
        }

        [Test]
        public void Test_CohortPackagerHost_HappyPath()
        {
            Guid jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = DateTime.UtcNow,
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 1,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
                ExtractFileMessagesDispatched = new JsonCompatibleDictionary<MessageHeader, string>
                {
                    { new MessageHeader(), "study-1-anon-1.dcm" },
                    { new MessageHeader(), "study-1-anon-2.dcm" },
                },
                RejectionReasons = new Dictionary<string, int>
                {
                    {"rejected - blah", 1 },
                },
                KeyValue = "study-1",
            };
            var testExtractFileStatusMessage = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                OutputFilePath = "study-1-anon-1.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
                Status = ExtractedFileStatus.ErrorWontRetry,
                StatusMessage = "Couldn't anonymise",
                DicomFilePath = "study-1-orig-1.dcm",
            };
            var testIsIdentifiableMessage = new ExtractedFileVerificationMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                OutputFilePath = "study-1-anon-2.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
                IsIdentifiable = false,
                Report = "[]",
                DicomFilePath = "study-1-orig-2.dcm",
            };


            GlobalOptions globals = new GlobalOptionsFactory().Load();
            globals.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 5;

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
                tester.SendMessage(globals.CohortPackagerOptions.FileCollectionInfoOptions, new MessageHeader(), testExtractFileCollectionInfoMessage);
                tester.SendMessage(globals.CohortPackagerOptions.NoVerifyStatusOptions, new MessageHeader(), testExtractFileStatusMessage);
                tester.SendMessage(globals.CohortPackagerOptions.VerificationStatusOptions, new MessageHeader(), testIsIdentifiableMessage);

                var reporter = new TestReporter();
                var notifier = new TestLoggingNotifier();
                var host = new CohortPackagerHost(
                    globals,
                    jobStore: null,
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

                // TODO(rkm 2020-10-02) Test actual reports content once split into new files
                Assert.True(reporter.ReportCreated);
            }
        }

        [Test]
        public void Test_CohortPackagerHost_IdentifiableExtraction()
        {
            Guid jobId = Guid.NewGuid();
            var testExtractionRequestInfoMessage = new ExtractionRequestInfoMessage
            {
                ExtractionModality = "MR",
                JobSubmittedAt = DateTime.UtcNow,
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
                KeyTag = "StudyInstanceUID",
                KeyValueCount = 1,
                IsIdentifiableExtraction = true,
            };
            var testExtractFileCollectionInfoMessage = new ExtractFileCollectionInfoMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
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
                JobSubmittedAt = DateTime.UtcNow,
                OutputFilePath = "src.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
                Status = ExtractedFileStatus.Copied,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-1.dcm",
                IsIdentifiableExtraction = true,
            };
            var testExtractFileStatusMessage2 = new ExtractedFileStatusMessage
            {
                JobSubmittedAt = DateTime.UtcNow,
                OutputFilePath = "src_missing.dcm",
                ProjectNumber = "testProj1",
                ExtractionJobIdentifier = jobId,
                ExtractionDirectory = _extractionDir,
                Status = ExtractedFileStatus.FileMissing,
                StatusMessage = null,
                DicomFilePath = "study-1-orig-2.dcm",
                IsIdentifiableExtraction = true,
            };

            GlobalOptions globals = new GlobalOptionsFactory().Load();
            globals.CohortPackagerOptions.JobWatcherTimeoutInSeconds = 5;

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
            mockFileSystem.Directory.CreateDirectory(Path.Combine(ExtractRoot, _projectExtractDir, "reports"));

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

            // TODO(rkm 2020-10-02) Test actual reports content once split into new files
            //List<string> allFiles = mockFileSystem.AllFiles.ToList();
            //Assert.AreEqual(1, allFiles.Count);
            //string reportContent = mockFileSystem.File.ReadAllText(allFiles[0]);
            //var expected = "";
            //Assert.AreEqual(expected, reportContent);
        }

        #endregion
    }
}
