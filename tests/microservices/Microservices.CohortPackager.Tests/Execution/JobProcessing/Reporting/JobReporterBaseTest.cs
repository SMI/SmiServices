using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Smi.Common.Options;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting
{
    // TODO(rkm 2020-11-19) Replace hard-coded JSON reports with Failure objects
    [TestFixture]
    public class JobReporterBaseTest
    {
        private const string WindowsNewLine = "\r\n";
        private const string LinuxNewLine = "\n";

        private static readonly TestDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();

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

        private class TestJobReporter : JobReporterBase
        {
            public string Report { get; set; } = "";

            public bool Disposed { get; set; }

            private string _currentReportName;
            private bool _isCombinedReport;

            public TestJobReporter(
                IExtractJobStore jobStore,
                ReportFormat reportFormat,
                string reportNewLine
            )
                : base(
                    jobStore,
                    reportFormat,
                    reportNewLine
                )
            {
            }

            protected override Stream GetStreamForSummary(ExtractJobInfo jobInfo)
            {
                _currentReportName = "summary";
                _isCombinedReport = ShouldWriteCombinedReport(jobInfo);
                return new MemoryStream();
            }

            protected override Stream GetStreamForPixelDataSummary(ExtractJobInfo jobInfo)
            {
                _currentReportName = "pixel summary";
                return new MemoryStream();
            }

            protected override Stream GetStreamForPixelDataFull(ExtractJobInfo jobInfo)
            {
                _currentReportName = "pixel full";
                return new MemoryStream();
            }

            protected override Stream GetStreamForPixelDataWordLengthFrequencies(ExtractJobInfo jobInfo)
            {
                _currentReportName = "pixel word length frequencies";
                return new MemoryStream();
            }

            protected override Stream GetStreamForTagDataSummary(ExtractJobInfo jobInfo)
            {
                _currentReportName = "tag summary";
                return new MemoryStream();
            }

            protected override Stream GetStreamForTagDataFull(ExtractJobInfo jobInfo)
            {
                _currentReportName = "tag full";
                return new MemoryStream();
            }

            protected override void FinishReportPart(Stream stream)
            {
                stream.Position = 0;
                using var streamReader = new StreamReader(stream, leaveOpen: true);
                string header = _isCombinedReport ? "" : $"{ReportNewLine}=== {_currentReportName} file ==={ReportNewLine}";
                Report += header + streamReader.ReadToEnd();
            }

            protected override void ReleaseUnmanagedResources() => Disposed = true;
            public override void Dispose() => ReleaseUnmanagedResources();
        }

        private static CompletedExtractJobInfo TestJobInfo(
            bool isIdentifiableExtraction = false,
            bool isNoFilterExtraction = false
        ) =>
            new CompletedExtractJobInfo(
                Guid.NewGuid(),
                _dateTimeProvider.UtcNow(),
                _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
                "1234",
                "extractions/test",
                "keyTag",
                123,
                null,
                isIdentifiableExtraction,
                isNoFilterExtraction
            );

        [TestCase(LinuxNewLine)]
        [TestCase(WindowsNewLine)]
        public void CreateReport_Empty(string newLine)
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<ExtractionIdentifierRejectionInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<FileAnonFailureInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(new List<FileVerificationFailureInfo>());

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, newLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var expectedLines = new List<string>
            {
                "# SMI extraction validation report for 1234/test",
                $"",
                $"Job info:",
                $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {TimeSpan.FromHours(1)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               keyTag",
                $"-   Extraction modality:          Unspecified",
                $"-   Requested identifier count:   123",
                $"-   Identifiable extraction:      No",
                $"-   Filtered extraction:          Yes",
                $"",
                $"Report contents:",
                $"",
                $"-   Verification failures",
                $"-   Blocked files",
                $"-   Anonymisation failures",
                $"",
                $"## Verification failures",
                $"",
                $"## Blocked files",
                $"",
                $"## Anonymisation failures",
                $"",
                $"--- end of report ---",
                $"",
            };

            Assert.AreEqual(string.Join(newLine, expectedLines), reporter.Report);
        }

        [TestCase(LinuxNewLine)]
        [TestCase(WindowsNewLine)]
        public void CreateReport_BasicData(string newLine)
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            var rejections = new List<ExtractionIdentifierRejectionInfo>
            {
                new ExtractionIdentifierRejectionInfo(
                    keyValue: "1.2.3.4",
                    new Dictionary<string, int>
                    {
                        {"image is in the deny list for extraction", 123},
                        {"foo bar", 456},
                    }),
            };

            var anonFailures = new List<FileAnonFailureInfo>
            {
                new FileAnonFailureInfo(expectedAnonFile: "foo1.dcm", reason: "image was corrupt"),
            };

            const string report = @"
[
    {
        'Parts': [{'Classification':'Person','Offset':0,'Word':'FOO'}],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'FOO'
    }
]";

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo1.dcm", report),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(rejections);
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(anonFailures);
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, newLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var expectedLines = new List<string>
            {
                "# SMI extraction validation report for 1234/test",
                $"",
                $"Job info:",
                $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {TimeSpan.FromHours(1)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               keyTag",
                $"-   Extraction modality:          Unspecified",
                $"-   Requested identifier count:   123",
                $"-   Identifiable extraction:      No",
                $"-   Filtered extraction:          Yes",
                $"",
                $"Report contents:",
                $"",
                $"-   Verification failures",
                $"-   Blocked files",
                $"-   Anonymisation failures",
                $"",
                $"## Verification failures",
                $"",
                $"-   Tag: ScanOptions (1 total issues(s))",
                $"    -   Value: 'FOO' (1 occurrence(s))",
                $"        With failures:",
                $"        -   'FOO' at offset 0 classified as Person",
                $"        In files:",
                $"        -   foo1.dcm",
                $"",
                $"## Blocked files",
                $"",
                $"-   ID: 1.2.3.4",
                $"    -   456x 'foo bar'",
                $"    -   123x 'image is in the deny list for extraction'",
                $"",
                $"## Anonymisation failures",
                $"",
                $"-   file 'foo1.dcm': 'image was corrupt'",
                $"--- end of report ---",
                $"",
            };

            Assert.AreEqual(string.Join(newLine, expectedLines), reporter.Report);
        }

        [Test]
        public void InvalidReport_ThrowsApplicationException()
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo1.dcm", failureData: "totally not a report"),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<ExtractionIdentifierRejectionInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<FileAnonFailureInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, LinuxNewLine))
            {
                Assert.Throws<ApplicationException>(() => reporter.CreateReport(Guid.Empty), "aa");
            }

            Assert.True(reporter.Disposed);
        }

        [TestCase(LinuxNewLine)]
        [TestCase(WindowsNewLine)]
        public void CreateReport_AggregateData(string newLine)
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(relativeOutputFilePath: "ccc/ddd/foo1.dcm", failureData: @"
                    [
                        {
                             'Parts': [{'Classification':'Person','Offset':0,'Word':'BAZ'}],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'SomeOtherTag',
                            'ProblemValue': 'BAZ'
                        }
                    ]"
                ),
                new FileVerificationFailureInfo(relativeOutputFilePath:"ccc/ddd/foo2.dcm",failureData: @"
                    [
                        {
                             'Parts': [{'Classification':'Person','Offset':0,'Word':'BAZ'}],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'SomeOtherTag',
                            'ProblemValue': 'BAZ'
                        }
                    ]"
                ),
                new FileVerificationFailureInfo(relativeOutputFilePath:"aaa/bbb/foo1.dcm", failureData:@"
                    [
                        {
                            'Parts': [{'Classification':'Person','Offset':0,'Word':'FOO'}],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'ScanOptions',
                            'ProblemValue': 'FOO'
                        }
                    ]"
                ),
                new FileVerificationFailureInfo(relativeOutputFilePath:"aaa/bbb/foo2.dcm",failureData: @"
                    [
                        {
                            'Parts': [{'Classification':'Person','Offset':0,'Word':'FOO'}],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'ScanOptions',
                            'ProblemValue': 'FOO'
                        }
                    ]"
                ),
                new FileVerificationFailureInfo(relativeOutputFilePath:"aaa/bbb/foo2.dcm", failureData: @"
                    [
                         {
                            'Parts': [{'Classification':'Person','Offset':0,'Word':'BAR'}],
                            'Resource': 'unused',
                            'ResourcePrimaryKey': 'unused',
                            'ProblemField': 'ScanOptions',
                            'ProblemValue': 'BAR'
                        }
                    ]"
                ),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<ExtractionIdentifierRejectionInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<FileAnonFailureInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>()))
                .Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, newLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var expectedLines = new List<string>
            {
                "# SMI extraction validation report for 1234/test",
                $"",
                $"Job info:",
                $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {TimeSpan.FromHours(1)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               keyTag",
                $"-   Extraction modality:          Unspecified",
                $"-   Requested identifier count:   123",
                $"-   Identifiable extraction:      No",
                $"-   Filtered extraction:          Yes",
                $"",
                $"Report contents:",
                $"",
                $"-   Verification failures",
                $"-   Blocked files",
                $"-   Anonymisation failures",
                $"",
                $"## Verification failures",
                $"",
                $"-   Tag: ScanOptions (3 total issues(s))",
                $"    -   Value: 'FOO' (2 occurrence(s))",
                $"        With failures:",
                $"        -   'FOO' at offset 0 classified as Person",
                $"        In files:",
                $"        -   aaa/bbb/foo1.dcm",
                $"        -   aaa/bbb/foo2.dcm",
                $"    -   Value: 'BAR' (1 occurrence(s))",
                $"        With failures:",
                $"        -   'BAR' at offset 0 classified as Person",
                $"        In files:",
                $"        -   aaa/bbb/foo2.dcm",
                $"",
                $"-   Tag: SomeOtherTag (2 total issues(s))",
                $"    -   Value: 'BAZ' (2 occurrence(s))",
                $"        With failures:",
                $"        -   'BAZ' at offset 0 classified as Person",
                $"        In files:",
                $"        -   ccc/ddd/foo1.dcm",
                $"        -   ccc/ddd/foo2.dcm",
                $"",
                $"## Blocked files",
                $"",
                $"## Anonymisation failures",
                $"",
                $"--- end of report ---",
                $"",
            };

            Assert.AreEqual(string.Join(newLine, expectedLines), reporter.Report);
        }

        [TestCase(LinuxNewLine)]
        [TestCase(WindowsNewLine)]
        public void CreateReport_WithPixelData(string newLine)
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            // NOTE(rkm 2020-08-25) Tests that the "Z" tag is ordered before PixelData, and that PixelData items are ordered by decreasing length not by occurrence
            const string report = @"
[
     {
        'Parts': [{'Classification':'Person','Offset':-1,'Word':'aaaaaaaaaaa'}],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'PixelData',
        'ProblemValue': 'aaaaaaaaaaa'
    },
    {
        'Parts': [
            {'Classification':'Person','Offset':-1,'Word':'Foo'},
            {'Classification':'Person','Offset':-1,'Word':'Bar'}
        ],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'PixelData',
        'ProblemValue': 'Dr Foo Bar'
    },    
    {
        'Parts': [{'Classification':'Person','Offset':0,'Word':'bar'}],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'Z',
        'ProblemValue': 'bar'
    },
]";

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo1.dcm", report),
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo2.dcm", report),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<ExtractionIdentifierRejectionInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<FileAnonFailureInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, newLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var expectedLines = new List<string>
            {
                "# SMI extraction validation report for 1234/test",
                $"",
                $"Job info:",
                $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {TimeSpan.FromHours(1)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               keyTag",
                $"-   Extraction modality:          Unspecified",
                $"-   Requested identifier count:   123",
                $"-   Identifiable extraction:      No",
                $"-   Filtered extraction:          Yes",
                $"",
                $"Report contents:",
                $"",
                $"-   Verification failures",
                $"-   Blocked files",
                $"-   Anonymisation failures",
                $"",
                $"## Verification failures",
                $"",
                $"-   Tag: Z (2 total issues(s))",
                $"    -   Value: 'bar' (2 occurrence(s))",
                $"        With failures:",
                $"        -   'bar' at offset 0 classified as Person",
                $"        In files:",
                $"        -   foo1.dcm",
                $"        -   foo2.dcm",
                $"",
                $"-   Tag: PixelData (4 total issues(s))",
                $"    -   Value: 'Dr Foo Bar' (2 occurrence(s))",
                $"        With failures:",
                $"        -   'Foo' at offset -1 classified as Person",
                $"        -   'Bar' at offset -1 classified as Person",
                $"        In files:",
                $"        -   foo1.dcm",
                $"        -   foo2.dcm",
                $"    -   Value: 'aaaaaaaaaaa' (2 occurrence(s))",
                $"        With failures:",
                $"        -   'aaaaaaaaaaa' at offset -1 classified as Person",
                $"        In files:",
                $"        -   foo1.dcm",
                $"        -   foo2.dcm",
                $"",
                $"## Blocked files",
                $"",
                $"## Anonymisation failures",
                $"",
                $"--- end of report ---",
                $"",
            };

            Assert.AreEqual(string.Join(newLine, expectedLines), reporter.Report);
        }

        [TestCase(LinuxNewLine)]
        [TestCase(WindowsNewLine)]
        public void CreateReport_IdentifiableExtraction(string newLine)
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo(isIdentifiableExtraction: true);

            var missingFiles = new List<string>
            {
               "missing.dcm",
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns(missingFiles);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, newLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var missingFilesExpected = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("missing.dcm", null),
            };

            var expectedLines = new List<string>
            {
                $"# SMI extraction validation report for 1234/test",
                $"",
                $"Job info:",
                $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {TimeSpan.FromHours(1)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               keyTag",
                $"-   Extraction modality:          Unspecified",
                $"-   Requested identifier count:   123",
                $"-   Identifiable extraction:      Yes",
                $"-   Filtered extraction:          Yes",
                $"",
                $"Report contents:",
                $"",
                $"-   Missing file list (files which were selected from an input ID but could not be found)",
                $"",
                $"## Missing file list",
                $"",
                $"-   missing.dcm",
                $"",
                $"--- end of report ---",
                $"",
            };

            Assert.AreEqual(string.Join(newLine, expectedLines), reporter.Report);
        }

        [TestCase(LinuxNewLine)]
        [TestCase(WindowsNewLine)]
        public void CreateReport_SplitReport(string newLine)
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            const string pixelFailureData = @"
[
     {
        'Parts': [
            {'Classification':'Person','Offset':-1,'Word':'aaaaaaaaaaa'}
        ],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'PixelData',
        'ProblemValue': 'aaaaaaaaaaa'
    },
]";

            const string tagFailureData1 = @"
[
     {
        'Parts': [
            {'Classification':'Person','Offset':3,'Word':'Foo\'s'}
        ],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'Dr Foo\'s protocol'
    },
    {
        'Parts': [
            {'Classification':'Person','Offset':0,'Word':'Foo'},
            {'Classification':'Person','Offset':4,'Word':'Bar'}
        ],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'SeriesDescription',
        'ProblemValue': 'Foo Bar'
    },
]";

            const string tagFailureData2 = @"
[
     {
        'Parts': [
            {'Classification':'Organization','Offset':2,'Word':'Hospital'}
        ],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'SeriesDescription',
        'ProblemValue': 'A Hospital'
    },
]";


            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo1.dcm", tagFailureData1),
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo1.dcm", pixelFailureData),
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo2.dcm", tagFailureData1),
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo2.dcm", pixelFailureData),
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo2.dcm", tagFailureData2),
                new FileVerificationFailureInfo(relativeOutputFilePath: "foo3.dcm", tagFailureData1),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<ExtractionIdentifierRejectionInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<FileAnonFailureInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Split, newLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var expectedLines = new List<string>
            {
                $"",
                $"=== summary file ===",
                $"# SMI extraction validation report for 1234/test",
                $"",
                $"Job info:",
                $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                $"-   Job duration:                 {TimeSpan.FromHours(1)}",
                $"-   Job extraction id:            {jobInfo.ExtractionJobIdentifier}",
                $"-   Extraction tag:               keyTag",
                $"-   Extraction modality:          Unspecified",
                $"-   Requested identifier count:   123",
                $"-   Identifiable extraction:      No",
                $"-   Filtered extraction:          Yes",
                $"",
                $"Files included:",
                $"-   README.md (this file)",
                $"-   pixel_data_summary.csv",
                $"-   pixel_data_full.csv",
                $"-   pixel_data_word_length_frequencies.csv",
                $"-   tag_data_summary.csv",
                $"-   tag_data_full.csv",
                $"",
                $"This file contents:",
                $"-   Blocked files",
                $"-   Anonymisation failures",
                $"",
                $"## Blocked files",
                $"",
                $"",
                $"## Anonymisation failures",
                $"",
                $"",
                $"--- end of report ---",
                $"",
                $"=== pixel summary file ===",
                $"TagName,FailureValue,Offset,Classification,Occurrences,RelativeFrequencyInTag,RelativeFrequencyInReport",
                $"PixelData,aaaaaaaaaaa,-1,Person,2,1,1",
                $"",
                $"=== pixel full file ===",
                $"TagName,FailureValue,Offset,Classification,FilePath",
                $"PixelData,aaaaaaaaaaa,-1,Person,foo1.dcm",
                $"PixelData,aaaaaaaaaaa,-1,Person,foo2.dcm",
                $"",
                $"=== pixel word length frequencies file ===",
                $"WordLength,Count,RelativeFrequencyInReport",
                $"1,0,0",
                $"2,0,0",
                $"3,0,0",
                $"4,0,0",
                $"5,0,0",
                $"6,0,0",
                $"7,0,0",
                $"8,0,0",
                $"9,0,0",
                $"10,0,0",
                $"11,2,1",
                $"",
                $"=== tag summary file ===",
                $"TagName,FailureValue,Offset,Classification,Occurrences,RelativeFrequencyInTag,RelativeFrequencyInReport",
                $"SeriesDescription,Foo,0,Person,3,0.42857142857142855,0.3",
                $"SeriesDescription,Bar,4,Person,3,0.42857142857142855,0.3",
                $"SeriesDescription,Hospital,2,Organization,1,0.14285714285714285,0.1",
                $"ScanOptions,Foo's,3,Person,3,1,0.3",
                $"",
                $"=== tag full file ===",
                $"TagName,FailureValue,Offset,Classification,FilePath",
                $"ScanOptions,Foo's,3,Person,foo1.dcm",
                $"ScanOptions,Foo's,3,Person,foo2.dcm",
                $"ScanOptions,Foo's,3,Person,foo3.dcm",
                $"SeriesDescription,Hospital,2,Organization,foo2.dcm",
                $"SeriesDescription,Foo,0,Person,foo1.dcm",
                $"SeriesDescription,Foo,0,Person,foo2.dcm",
                $"SeriesDescription,Foo,0,Person,foo3.dcm",
                $"SeriesDescription,Bar,4,Person,foo1.dcm",
                $"SeriesDescription,Bar,4,Person,foo2.dcm",
                $"SeriesDescription,Bar,4,Person,foo3.dcm",
                $"",
            };

            Assert.AreEqual(string.Join(newLine, expectedLines), reporter.Report);
        }

        [Test]
        public void Constructor_UnknownReportFormat_ThrowsArgumentException()
        {
            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            Assert.Throws<ArgumentException>(() =>
            {
                var _ = new TestJobReporter(mockJobStore.Object, ReportFormat.Unknown, "foo");
            });
        }

        [Test]
        public void Constructor_NoNewLine_SetToEnvironment()
        {
            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            var reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Split, null);
            Assert.AreEqual(Environment.NewLine, reporter.ReportNewLine);
        }
        
        [Test]
        public void ReportNewLine_LoadFromYaml_EscapesNewlines()
        {
            string yaml = @"
LoggingOptions:
    LogConfigFile:
CohortPackagerOptions:
    ReportNewLine: '\r\n'
";
            string tmpConfig = Path.GetTempFileName() + ".yaml";
            File.WriteAllText(tmpConfig, yaml);
            GlobalOptions globals = new GlobalOptionsFactory().Load(nameof(ReportNewLine_LoadFromYaml_EscapesNewlines), tmpConfig);

            // NOTE(rkm 2021-04-06) Verify we get an *escaped* newline from the YAML load here
            Assert.AreEqual(Regex.Escape(WindowsNewLine), globals.CohortPackagerOptions.ReportNewLine);
        }

        [Test]
        public void ReportNewline_EscapedString_IsDetected()
        {
            const string newLine = @"\n";
            var exc = Assert.Throws<ArgumentException>(() => new TestJobReporter(new Mock<IExtractJobStore>().Object, ReportFormat.Combined, newLine));
            Assert.AreEqual("ReportNewLine contained an escaped backslash", exc.Message);
        }
    }

    #endregion
}
