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
using System.Linq.Expressions;

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
        private static ExtractJobInfo GetSampleExtractJobInfo()
           => new(
               Guid.NewGuid(),
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

        #endregion

        #region Tests

        [Test]
        public void ProcessJobs_HappyPath()
        {
            // Arrange

            var jobInfo = GetSampleExtractJobInfo();

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            Expression<Func<IExtractJobStore, List<ExtractJobInfo>>> getReadyJobsCall = x => x.GetReadyJobs(jobInfo.ExtractionJobIdentifier);
            mockJobStore.Setup(getReadyJobsCall).Returns(new List<ExtractJobInfo> { jobInfo });
            Expression<Action<IExtractJobStore>> markJobCompletedCall = x => x.MarkJobCompleted(jobInfo.ExtractionJobIdentifier);
            mockJobStore.Setup(markJobCompletedCall);

            var mockNotifier = new Mock<IJobCompleteNotifier>(MockBehavior.Strict);
            Expression<Action<IJobCompleteNotifier>> notifyJobCompletedCall = x => x.NotifyJobCompleted(jobInfo);
            mockNotifier.Setup(notifyJobCompletedCall);

            var mockReporter = new Mock<IJobReporter>(MockBehavior.Strict);
            Expression<Action<IJobReporter>> createReportsCall = x => x.CreateReports(jobInfo.ExtractionJobIdentifier);
            mockReporter.Setup(createReportsCall);

            var callbackUsed = false;
            var watcher = new ExtractJobWatcher(
                new CohortPackagerOptions { JobWatcherTimeoutInSeconds = 123 },
                mockJobStore.Object,
                new Action<Exception>(_ => callbackUsed = true),
                mockNotifier.Object,
                mockReporter.Object
            );

            // Act

            watcher.ProcessJobs(jobInfo.ExtractionJobIdentifier);

            // Assert

            Assert.False(callbackUsed);
            mockJobStore.Verify(getReadyJobsCall, Times.Once);
            mockJobStore.Verify(markJobCompletedCall, Times.Once);
            mockNotifier.Verify(notifyJobCompletedCall, Times.Once);
            mockReporter.Verify(createReportsCall, Times.Once);
        }

        [Test]
        public void ProcessJobs_DefaultGuid_ProcessesAll()
        {
            // Arrange

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            var jobs = new List<ExtractJobInfo>()
        {
            GetSampleExtractJobInfo(),
            GetSampleExtractJobInfo(),
        };
            mockJobStore.Setup(x => x.GetReadyJobs(default)).Returns(jobs);
            mockJobStore.Setup(x => x.MarkJobCompleted(It.IsAny<Guid>()));
            var mockReporter = new Mock<IJobReporter>(MockBehavior.Strict);
            mockReporter.Setup(x => x.CreateReports(It.IsAny<Guid>()));
            var mockNotifier = new Mock<IJobCompleteNotifier>(MockBehavior.Strict);
            mockNotifier.Setup(x => x.NotifyJobCompleted(It.IsAny<ExtractJobInfo>()));
            var opts = new CohortPackagerOptions { JobWatcherTimeoutInSeconds = 123 };
            var callbackUsed = false;
            var watcher = new ExtractJobWatcher(
                opts,
                mockJobStore.Object,
                new Action<Exception>(_ => callbackUsed = true),
                mockNotifier.Object,
                mockReporter.Object
            );

            // Act

            watcher.ProcessJobs();

            // Assert

            Assert.False(callbackUsed);
            mockJobStore.VerifyAll();
        }

        [Test]
        public void ProcessJobs_HandlesApplicationException()
        {
            // Arrange

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            var jobInfo = GetSampleExtractJobInfo();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns(new List<ExtractJobInfo> { jobInfo });
            mockJobStore.Setup(x => x.MarkJobCompleted(It.IsAny<Guid>())).Throws(new ApplicationException());
            mockJobStore.Setup(x => x.MarkJobFailed(jobInfo.ExtractionJobIdentifier, It.IsAny<ApplicationException>()));
            var mockNotifier = new Mock<IJobCompleteNotifier>(MockBehavior.Strict);
            var mockReporter = new Mock<IJobReporter>(MockBehavior.Strict);
            var opts = new CohortPackagerOptions { JobWatcherTimeoutInSeconds = 123 };
            var callbackUsed = false;
            var watcher = new ExtractJobWatcher(
                opts,
                mockJobStore.Object,
                new Action<Exception>(_ => callbackUsed = true),
                mockNotifier.Object,
                mockReporter.Object
            );

            // Act

            watcher.ProcessJobs(jobInfo.ExtractionJobIdentifier);

            // Assert

            Assert.False(callbackUsed);
            mockJobStore.VerifyAll();
        }

        [Test]
        public void ProcessJobs_UncaughtExceptions_UsesCallback()
        {
            // Arrange

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            var jobInfo = GetSampleExtractJobInfo();
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns(new List<ExtractJobInfo> { jobInfo });
            mockJobStore.Setup(x => x.GetReadyJobs(It.IsAny<Guid>())).Returns(new List<ExtractJobInfo> { jobInfo });
            mockJobStore.Setup(x => x.MarkJobCompleted(It.IsAny<Guid>())).Throws(new Exception("aah"));
            var mockNotifier = new Mock<IJobCompleteNotifier>(MockBehavior.Strict);
            var mockReporter = new Mock<IJobReporter>(MockBehavior.Strict);
            var opts = new CohortPackagerOptions { JobWatcherTimeoutInSeconds = 123 };
            var callbackUsed = false;
            var watcher = new ExtractJobWatcher(
              opts,
              mockJobStore.Object,
              new Action<Exception>(_ => callbackUsed = true),
              mockNotifier.Object,
              mockReporter.Object
            );

            // Act

            watcher.ProcessJobs(jobInfo.ExtractionJobIdentifier);

            // Assert

            Assert.True(callbackUsed);
        }

        #endregion
    }
}
