using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            string expected = $@"
# SMI file extraction report for 1234

Job info:
-    Job submitted at:              {provider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}
-    Job extraction id:             {jobId}
-    Extraction tag:                keyTag
-    Extraction modality:           ZZ
-    Requested identifier count:    123

Report contents:
-    Verification failures
-    Rejected failures
-    Anonymisation failures

## Verification failures


## Rejected files


## Anonymisation failures


--- end of report ---
";
            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }

        [Test]
        public void Test_JobReporterBase_CreateReport_WithBasicData()
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
            };

            const string report = @"
[
    {
        'Parts': [],
        'Resource': '/foo1.dcm',
        'ResourcePrimaryKey': '1.2.3.4',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'FOO'
    }
]";

            var verificationFailures = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("foo1.dcm", report),
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

            string expected = $@"
# SMI file extraction report for 1234

Job info:
-    Job submitted at:              {provider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}
-    Job extraction id:             {jobId}
-    Extraction tag:                keyTag
-    Extraction modality:           ZZ
-    Requested identifier count:    123

Report contents:
-    Verification failures
-    Rejected failures
-    Anonymisation failures

## Verification failures

- Tag: ScanOptions (1 total occurrence(s))
    - Value: 'FOO' (1 occurrence(s))
        - foo1.dcm


## Rejected files

- ID: 1.2.3.4
    - 456x 'foo bar'
    - 123x 'image is in the deny list for extraction'

## Anonymisation failures

- file 'foo1.dcm': 'image was corrupt'

--- end of report ---
";

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

        [Test]
        public void Test_JobReporterBase_CreateReport_AggregateData()
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
                new Tuple<string, string>("ccc/ddd/foo1.dcm", @"
                    [
                        {
                             'Parts': [],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'SomeOtherTag',
                            'ProblemValue': 'BAZ'
                        }
                    ]"
                ),
                new Tuple<string, string>("ccc/ddd/foo2.dcm", @"
                    [
                        {
                             'Parts': [],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'SomeOtherTag',
                            'ProblemValue': 'BAZ'
                        }
                    ]"
                ),
                new Tuple<string, string>("aaa/bbb/foo1.dcm", @"
                    [
                        {
                            'Parts': [],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'ScanOptions',
                            'ProblemValue': 'FOO'
                        }
                    ]"
                ),
                new Tuple<string, string>("aaa/bbb/foo2.dcm", @"
                    [
                        {
                            'Parts': [],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'ScanOptions',
                            'ProblemValue': 'FOO'
                        }
                    ]"
                ),
                new Tuple<string, string>("aaa/bbb/foo2.dcm", @"
                    [
                         {
                            'Parts': [],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'ScanOptions',
                            'ProblemValue': 'BAR'
                        }
                    ]"
                ),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<Tuple<string, Dictionary<string, int>>>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>()))
                .Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object))
            {
                reporter.CreateReport(Guid.Empty);
            }

            string expected = $@"
# SMI file extraction report for 1234

Job info:
-    Job submitted at:              {provider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}
-    Job extraction id:             {jobId}
-    Extraction tag:                keyTag
-    Extraction modality:           ZZ
-    Requested identifier count:    123

Report contents:
-    Verification failures
-    Rejected failures
-    Anonymisation failures

## Verification failures

- Tag: ScanOptions (3 total occurrence(s))
    - Value: 'FOO' (2 occurrence(s))
        - aaa/bbb/foo1.dcm
        - aaa/bbb/foo2.dcm

    - Value: 'BAR' (1 occurrence(s))
        - aaa/bbb/foo2.dcm

- Tag: SomeOtherTag (2 total occurrence(s))
    - Value: 'BAZ' (2 occurrence(s))
        - ccc/ddd/foo1.dcm
        - ccc/ddd/foo2.dcm


## Rejected files


## Anonymisation failures


--- end of report ---
";
            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }
    }

    #endregion
}
