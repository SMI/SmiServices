using Moq;
using NUnit.Framework;
using SmiServices.Common.Options;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.Microservices.CohortPackager.JobProcessing;
using SmiServices.Microservices.CohortPackager.JobProcessing.Notifying;
using SmiServices.Microservices.CohortPackager.JobProcessing.Reporting;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;

namespace SmiServices.UnitTests.Microservices.CohortPackager.Execution.JobProcessing
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
            public void CreateReports(Guid jobId)
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
                "testUser",
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
            mockJobStore.Setup(x => x.GetReadyJobs(default)).Returns([]);
            watcher.ProcessJobs();
            mockJobStore.Verify();

            // Check that we MarkJobFailed for known exceptions
            mockJobStore.Reset();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns([testJobInfo]);
            mockJobStore.Setup(x => x.MarkJobCompleted(It.IsAny<Guid>())).Throws(new ApplicationException("aah"));
            watcher.ProcessJobs(jobId);
            mockJobStore.Verify(x => x.MarkJobFailed(jobId, It.IsAny<ApplicationException>()), Times.Once);

            // Check that we call the exception callback for unhandled exceptions
            mockJobStore.Reset();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns([testJobInfo]);
            mockJobStore.Setup(x => x.MarkJobCompleted(It.IsAny<Guid>())).Throws(new Exception("aah"));
            watcher.ProcessJobs(jobId);
            Assert.That(callbackUsed, Is.True);

            // Check happy path
            mockJobStore.Reset();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns([testJobInfo]);
            testNotifier.Notified = false;
            watcher.ProcessJobs(jobId);
            Assert.Multiple(() =>
            {
                Assert.That(testNotifier.Notified, Is.True);
                Assert.That(testReporter.Reported, Is.True);
            });
        }

        #endregion
    }
}
