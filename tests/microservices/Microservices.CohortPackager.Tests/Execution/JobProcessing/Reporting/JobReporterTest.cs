using System;
using System.IO;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;


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

            var mockJobStore = new Mock<IExtractJobStore>();
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object))
            {
                reporter.CreateReport(Guid.Empty);
            }

            string nl = Environment.NewLine;
            string expected = $@"
Extraction completion report for job {jobId}:
    Job submitted at:              {provider.UtcNow()}
    Project number:                1234
    Extraction tag:                keyTag
    Extraction modality:           ZZ
    Requested identifier count:    123

Rejected files:

Anonymisation failures:
Expected anonymised file | Failure reason

Verification failures:
Anonymised file | Failure reason

";

            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }

        #endregion
    }
}
