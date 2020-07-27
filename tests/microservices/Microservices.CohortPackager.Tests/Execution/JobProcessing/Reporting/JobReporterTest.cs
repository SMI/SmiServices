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
        public void Test_JobReporterBase_CreateReport_Empty()
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
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<Tuple<string, Dictionary<string, int>>>());
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


## Verification failures


";

            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }

        [Test]
        public void Test_JobReporterBase_CreateReport_WithData()
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

            var rejections = new List<Tuple<string, Dictionary<string, int>>>
            {
                new Tuple<string, Dictionary<string, int>>(
                    "1.2.3.4",
                    new Dictionary<string, int>
                    {
                        {"image is in the deny list for extraction", 123},
                        {"foo bar", 456},
                    }),
            };

            var anonFailures = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("foo1.dcm", "image was corrupt"),
                new Tuple<string, string>("foo2.dcm", "could not be anonymised"),
            };

            const string report = @"
[
    {
        'Parts': [
            {
                'Classification': 1,
                'Offset': 0,
                'Word': 'FOO'
            },
            {
                'Classification': 3,
                'Offset': 1,
                'Word': 'BAR'
            }
        ],
        'Resource': '/foo.dcm',
        'ResourcePrimaryKey': '1.2.3.4',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'FOO'
    }
]";

            var verificationFailures = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("foo1.dcm", report),
                new Tuple<string, string>("foo2.dcm", report),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(rejections);
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(anonFailures);
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

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

- ID '1.2.3.4':
    - 123x 'image is in the deny list for extraction'
    - 456x 'foo bar'

## Anonymisation failures

- file 'foo1.dcm': 'image was corrupt'
- file 'foo2.dcm': 'could not be anonymised'

## Verification failures

- file 'foo1.dcm':
      (Problem Field | Problem Value)
    - ScanOptions | FOO
         (Classification | Offset | Word)
       - PrivateIdentifier | 0 | FOO
       - Person | 1 | BAR

- file 'foo2.dcm':
      (Problem Field | Problem Value)
    - ScanOptions | FOO
         (Classification | Offset | Word)
       - PrivateIdentifier | 0 | FOO
       - Person | 1 | BAR


";
            Console.WriteLine(reporter.Report);
            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }

        [Test]
        public void Test_JobReporterBase_WriteJobVerificationFailures_JsonException()
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

            var verificationFailures = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("foo.dcm", "totally not a report"),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<Tuple<string, Dictionary<string, int>>>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object))
            {
                Assert.Throws<ApplicationException>(() => reporter.CreateReport(Guid.Empty), "aa");
            }

            Assert.True(reporter.Disposed);
        }

        #endregion
    }
}
