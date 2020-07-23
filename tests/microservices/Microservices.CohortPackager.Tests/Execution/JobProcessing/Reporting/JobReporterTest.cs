using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.IO;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting
{
    [TestFixture]
    public class JobReporterTest
    {
        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp() { }

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

        private class TestJobReporter : JobReporterBase
        {
            public string Report { get; set; }

            public bool Disposed { get; set; }

            public TestJobReporter(IExtractJobStore jobStore)
                : base(jobStore, null) { }

            protected override Stream GetStream(Guid jobId)
            {
                return new MemoryStream();
            }

            protected override void FinishReport(Stream stream)
            {
                using (var streamReader = new StreamReader(stream))
                {
                    Report = streamReader.ReadToEnd();
                }
            }


            protected override void ReleaseUnmanagedResources()
            {
                Disposed = true;
            }


            public override void Dispose()
            {
                ReleaseUnmanagedResources();
            }
        }

        [Test]
        public void TestJobReporter_Base()
        {
            Guid jobId = Guid.NewGuid();
            var provider = new TestDateTimeProvider();
            var testJobInfo = new ExtractJobInfo(
                jobId,
                provider.UtcNow(),
                "1234",
                "test/dir",
                "keyTag",
                123,
                "ZZ",
                ExtractJobStatus.Completed);
            
            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<Tuple<string, int>>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object))
            {
                reporter.CreateReport(Guid.Empty);
            }

            string nl = Environment.NewLine;
            string expected = $@"
# SMI file extraction report for 1234
    Job submitted at:              {provider.UtcNow()}
    Job extraction id:             {jobId}
    Extraction tag:                keyTag
    Extraction modality:           ZZ
    Requested identifier count:    123

## Rejected files

## Anonymisation failures
Expected anonymised file | Failure reason

## Verification failures
Problem Field | Problem Value | Parts

";

            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }

        #endregion
    }
}
