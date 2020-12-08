using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing;
using Microservices.CohortPackager.Execution.JobProcessing.Notifying;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;

namespace Microservices.CohortPackager.Tests.Execution.JobProcessing
{
    [TestFixture]
    public class ExtractJobWatcherTest
    {
        #region Fixture Methods

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
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        private class TestJobCompleteNotifier : IJobCompleteNotifier
        {
            public bool Notified { get; set; }

            public void NotifyJobCompleted(ExtractJobInfo jobInfo)
            {
                Notified = true;
            }
        }

        private class TestJobReporter : IJobReporter
        {
            public bool Reported { get; set; }
            public void CreateReport(Guid jobId)
            {
                Reported = true;
            }
        }

        [Test]
        public void TestProcessJobs()
        {
            Guid jobId = Guid.NewGuid();
            var testJobInfo = new ExtractJobInfo(
                jobId,
                DateTime.UtcNow,
                "123",
                "test/dir",
                "KeyTag",
                123,
                null,
                ExtractJobStatus.ReadyForChecks,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: true
            );

            var opts = new CohortPackagerOptions { JobWatcherTimeoutInSeconds = 123 };
            var mockJobStore = new Mock<IExtractJobStore>();
            var callbackUsed = false;
            var mockCallback = new Action<Exception>(_ => callbackUsed = true);
            var testNotifier = new TestJobCompleteNotifier();
            var testReporter = new TestJobReporter();

            var watcher = new ExtractJobWatcher(opts, mockJobStore.Object, mockCallback, testNotifier, testReporter);

            // Check that we can call ProcessJobs with no Guid to process all jobs
            mockJobStore.Setup(x => x.GetReadyJobs(default(Guid))).Returns(new List<ExtractJobInfo>());
            watcher.ProcessJobs();
            mockJobStore.Verify();

            // Check that we MarkJobFailed for known exceptions
            mockJobStore.Reset();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns(new List<ExtractJobInfo> { testJobInfo });
            mockJobStore.Setup(x => x.MarkJobCompleted(It.IsAny<Guid>())).Throws(new ApplicationException("aah"));
            watcher.ProcessJobs(jobId);
            mockJobStore.Verify(x => x.MarkJobFailed(jobId, It.IsAny<ApplicationException>()), Times.Once);

            // Check that we call the exception callback for unhandled exceptions
            mockJobStore.Reset();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns(new List<ExtractJobInfo> { testJobInfo });
            mockJobStore.Setup(x => x.MarkJobCompleted(It.IsAny<Guid>())).Throws(new Exception("aah"));
            watcher.ProcessJobs(jobId);
            Assert.True(callbackUsed);

            // Check happy path
            mockJobStore.Reset();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns(new List<ExtractJobInfo> { testJobInfo });
            testNotifier.Notified = false;
            watcher.ProcessJobs(jobId);
            Assert.True(testNotifier.Notified);
            Assert.True(testReporter.Reported);
        }

        #endregion
    }
}
