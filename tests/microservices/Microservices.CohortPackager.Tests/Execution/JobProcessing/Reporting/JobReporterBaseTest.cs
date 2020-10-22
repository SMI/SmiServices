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
    public class JobReporterBaseTest
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

            public TestJobReporter(IExtractJobStore jobStore, ReportFormat reportFormat) : base(jobStore, reportFormat) { }

            protected override Stream GetStreamForSummary(ExtractJobInfo jobInfo) => new MemoryStream();
            protected override Stream GetStreamForPixelDataSummary(ExtractJobInfo jobInfo) => new MemoryStream();
            protected override Stream GetStreamForPixelDataFull(ExtractJobInfo jobInfo) => new MemoryStream();
            protected override Stream GetStreamForTagDataSummary(ExtractJobInfo jobInfo) => new MemoryStream();
            protected override Stream GetStreamForTagDataFull(ExtractJobInfo jobInfo) => new MemoryStream();

            protected override void FinishReportPart(Stream stream)
            {
                stream.Position = 0;
                using var streamReader = new StreamReader(stream);
                Report = streamReader.ReadToEnd();
            }

            protected override void ReleaseUnmanagedResources() => Disposed = true;
            public override void Dispose() => ReleaseUnmanagedResources();
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
                ExtractJobStatus.Completed,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: false
                );

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<Tuple<string, Dictionary<string, int>>>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined))
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
-    Identifiable extraction:       No
-    Filtered extraction:           Yes

Report contents:
-    Verification failures
    -    Summary
    -    Full Details
-    Blocked files
-    Anonymisation failures

## Verification failures

### Summary


### Full details


## Blocked files


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
                ExtractJobStatus.Completed,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: false
                );

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
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined))
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
-    Identifiable extraction:       No
-    Filtered extraction:           Yes

Report contents:
-    Verification failures
    -    Summary
    -    Full Details
-    Blocked files
-    Anonymisation failures

## Verification failures

### Summary

- Tag: ScanOptions (1 total occurrence(s))
    - Value: 'FOO' (1 occurrence(s))


### Full details

- Tag: ScanOptions (1 total occurrence(s))
    - Value: 'FOO' (1 occurrence(s))
        - foo1.dcm


## Blocked files

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
                ExtractJobStatus.Completed,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: false
                );

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
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined))
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
                ExtractJobStatus.Completed,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: false
                );

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
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined))
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
-    Identifiable extraction:       No
-    Filtered extraction:           Yes

Report contents:
-    Verification failures
    -    Summary
    -    Full Details
-    Blocked files
-    Anonymisation failures

## Verification failures

### Summary

- Tag: ScanOptions (3 total occurrence(s))
    - Value: 'FOO' (2 occurrence(s))
    - Value: 'BAR' (1 occurrence(s))

- Tag: SomeOtherTag (2 total occurrence(s))
    - Value: 'BAZ' (2 occurrence(s))


### Full details

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


## Blocked files


## Anonymisation failures


--- end of report ---
";
            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }

        [Test]
        public void Test_JobReporterBase_CreateReport_WithPixelData()
        {
            // NOTE(rkm 2020-08-25) Tests that the "Z" tag is ordered before PixelData, and that PixelData items are ordered by decreasing length not by occurrence

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
                ExtractJobStatus.Completed,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: false
                );

            const string report = @"
[
     {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'PixelData',
        'ProblemValue': 'aaaaaaaaaaa'
    },
    {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'PixelData',
        'ProblemValue': 'a'
    },
    {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'PixelData',
        'ProblemValue': 'a'
    },
    {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'Z',
        'ProblemValue': 'bar'
    },
]";

            var verificationFailures = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("foo1.dcm", report),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<Tuple<string, Dictionary<string, int>>>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined))
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
-    Identifiable extraction:       No
-    Filtered extraction:           Yes

Report contents:
-    Verification failures
    -    Summary
    -    Full Details
-    Blocked files
-    Anonymisation failures

## Verification failures

### Summary

- Tag: Z (1 total occurrence(s))
    - Value: 'bar' (1 occurrence(s))

- Tag: PixelData (3 total occurrence(s))
    - Value: 'aaaaaaaaaaa' (1 occurrence(s))
    - Value: 'a' (2 occurrence(s))


### Full details

- Tag: Z (1 total occurrence(s))
    - Value: 'bar' (1 occurrence(s))
        - foo1.dcm

- Tag: PixelData (3 total occurrence(s))
    - Value: 'aaaaaaaaaaa' (1 occurrence(s))
        - foo1.dcm
    - Value: 'a' (2 occurrence(s))
        - foo1.dcm
        - foo1.dcm


## Blocked files


## Anonymisation failures


--- end of report ---
";
            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }

        [Test]
        public void Test_JobReporterBase_CreateReport_IdentifiableExtraction()
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
                ExtractJobStatus.Completed,
                isIdentifiableExtraction: true,
                isNoFilterExtraction: false
                );

            var missingFiles = new List<string>
            {
               "missing.dcm",
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns(missingFiles);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined))
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
-    Identifiable extraction:       Yes
-    Filtered extraction:           Yes

Report contents:
-    Missing file list (files which were selected from an input ID but could not be found)

## Missing file list

-    missing.dcm

--- end of report ---
";
            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }


        [Test]
        public void Test_JobReporterBase_CreateReport_FilteredExtraction()
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
                ExtractJobStatus.Completed,
                isIdentifiableExtraction: false,
                isNoFilterExtraction: true
                );

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(testJobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<Tuple<string, Dictionary<string, int>>>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(new List<Tuple<string, string>>());

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined))
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
-    Identifiable extraction:       No
-    Filtered extraction:           No

Report contents:
-    Verification failures
    -    Summary
    -    Full Details
-    Blocked files
-    Anonymisation failures

## Verification failures

### Summary


### Full details


## Blocked files


## Anonymisation failures


--- end of report ---
";
            TestHelpers.AreEqualIgnoringCaseAndLineEndings(expected, reporter.Report);
            Assert.True(reporter.Disposed);
        }
    }

    #endregion
}
