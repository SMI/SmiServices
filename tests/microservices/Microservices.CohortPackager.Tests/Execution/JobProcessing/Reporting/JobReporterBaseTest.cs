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

        private static readonly TestDateTimeProvider _dateTimeProvider = new();

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
            new(
                Guid.NewGuid(),
                _dateTimeProvider.UtcNow(),
                _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
                "1234",
                "extractions/test",
                "keyTag",
                123,
                "testUser",
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

            ReportEqualityHelpers.AssertReportsAreEqual(
                jobInfo,
                _dateTimeProvider,
                verificationFailuresExpected: null,
                blockedFilesExpected: null,
                anonFailuresExpected: null,
                isIdentifiableExtraction: false,
                isJoinedReport: false,
                newLine,
                reporter.Report
            );
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
        'Parts': [],
        'Resource': '/foo1.dcm',
        'ResourcePrimaryKey': '1.2.3.4',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'FOO'
    }
]";

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(anonFilePath: "foo1.dcm", report),
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

            var verificationFailuresExpected = new Dictionary<string, Dictionary<string, List<string>>>
            {
                {
                    "ScanOptions", new Dictionary<string, List<string>>
                    {
                        {
                            "FOO",
                            new List<string>
                            {
                                "foo1.dcm"
                            }
                        }
                    }
                },
            };
            var blockedFilesExpected = new Dictionary<string, List<Tuple<int, string>>>
            {
                {
                    "1.2.3.4",
                    new List<Tuple<int, string>>
                    {
                        new Tuple<int, string>(123, "image is in the deny list for extraction"),
                        new Tuple<int, string>(456, "foo bar"),
                    }
                },
            };
            var anonFailuresExpected = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("foo1.dcm", "image was corrupt"),
            };

            Assert.True(reporter.Disposed);

            ReportEqualityHelpers.AssertReportsAreEqual(
                jobInfo,
                _dateTimeProvider,
                verificationFailuresExpected,
                blockedFilesExpected,
                anonFailuresExpected,
                isIdentifiableExtraction: false,
                isJoinedReport: false,
                newLine,
                reporter.Report
            );
        }

        [Test]
        public void InvalidReport_ThrowsApplicationException()
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(anonFilePath: "foo1.dcm", failureData: "totally not a report"),
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
                new FileVerificationFailureInfo(anonFilePath: "ccc/ddd/foo1.dcm", failureData: @"
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
                new FileVerificationFailureInfo(anonFilePath:"ccc/ddd/foo2.dcm",failureData: @"
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
                new FileVerificationFailureInfo(anonFilePath:"aaa/bbb/foo1.dcm", failureData:@"
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
                new FileVerificationFailureInfo(anonFilePath:"aaa/bbb/foo2.dcm",failureData: @"
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
                new FileVerificationFailureInfo(anonFilePath:"aaa/bbb/foo2.dcm", failureData: @"
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

            var verificationFailuresExpected = new Dictionary<string, Dictionary<string, List<string>>>
            {
                {
                    "ScanOptions", new Dictionary<string, List<string>>
                    {
                        {
                            "FOO",
                            new List<string>
                            {
                                "aaa/bbb/foo1.dcm",
                                "aaa/bbb/foo2.dcm",
                            }
                        },
                        {
                            "BAR",
                            new List<string>
                            {
                                "aaa/bbb/foo2.dcm",
                            }
                        },
                    }
                },
                {
                    "SomeOtherTag", new Dictionary<string, List<string>>
                    {
                        {
                            "BAZ",
                            new List<string>
                            {
                                "ccc/ddd/foo1.dcm",
                                "ccc/ddd/foo2.dcm",
                            }
                        },
                    }
                },
            };

            ReportEqualityHelpers.AssertReportsAreEqual(
                jobInfo,
                _dateTimeProvider,
                verificationFailuresExpected,
                blockedFilesExpected: null,
                anonFailuresExpected: null,
                isIdentifiableExtraction: false,
                isJoinedReport: false,
                newLine,
                reporter.Report
            );
        }

        [Test]
        public void CreateReport_WithPixelData()
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

            // NOTE(rkm 2020-08-25) Tests that the "Z" tag is ordered before PixelData, and that PixelData items are ordered by decreasing length not by occurrence
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

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(anonFilePath: "foo1.dcm", report),
            };

            var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
            mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
            mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(new List<ExtractionIdentifierRejectionInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(new List<FileAnonFailureInfo>());
            mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

            TestJobReporter reporter;
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, LinuxNewLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var verificationFailuresExpected = new Dictionary<string, Dictionary<string, List<string>>>
            {
                {
                    "Z", new Dictionary<string, List<string>>
                    {
                        {
                            "bar",
                            new List<string>
                            {
                                "foo1.dcm"
                            }
                        }
                    }
                },
                {
                    "PixelData", new Dictionary<string, List<string>>
                    {
                        {
                            "aaaaaaaaaaa",
                            new List<string>
                            {
                                "foo1.dcm"
                            }
                        },
                        {
                            "a",
                            new List<string>
                            {
                                "foo1.dcm",
                                "foo1.dcm"
                            }
                        },
                    }
                },
            };

            ReportEqualityHelpers.AssertReportsAreEqual(
                jobInfo,
                _dateTimeProvider,
                verificationFailuresExpected,
                blockedFilesExpected: null,
                anonFailuresExpected: null,
                isIdentifiableExtraction: false,
                isJoinedReport: false,
                LinuxNewLine,
                reporter.Report
            );
        }

        [Test]
        public void CreateReport_IdentifiableExtraction()
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
            using (reporter = new TestJobReporter(mockJobStore.Object, ReportFormat.Combined, LinuxNewLine))
            {
                reporter.CreateReport(Guid.Empty);
            }

            Assert.True(reporter.Disposed);

            var missingFilesExpected = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("missing.dcm", null),
            };

            ReportEqualityHelpers.AssertReportsAreEqual(
                jobInfo,
                _dateTimeProvider,
                verificationFailuresExpected: null,
                blockedFilesExpected: null,
                missingFilesExpected,
                isIdentifiableExtraction: true,
                isJoinedReport: false,
                LinuxNewLine,
                reporter.Report
            );
        }

        [TestCase(LinuxNewLine)]
        [TestCase(WindowsNewLine)]
        public void CreateReport_SplitReport(string newLine)
        {
            CompletedExtractJobInfo jobInfo = TestJobInfo();

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
        'ProblemField': 'PixelData',
        'ProblemValue': 'another'
    },
    {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'X',
        'ProblemValue': 'foo'
    },
    {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'X',
        'ProblemValue': 'foo'
    },
    {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'X',
        'ProblemValue': 'bar'
    },
    {
        'Parts': [],
        'Resource': 'unused',
        'ResourcePrimaryKey': 'unused',
        'ProblemField': 'Z',
        'ProblemValue': 'bar'
    },
]";

            var verificationFailures = new List<FileVerificationFailureInfo>
            {
                new FileVerificationFailureInfo(anonFilePath: "foo1.dcm", report),
                new FileVerificationFailureInfo(anonFilePath: "foo2.dcm", report),
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

            var expected = new List<string>
            {
                $"",
                "=== summary file ===",
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
                $"-   User name:                    {jobInfo.UserName}",
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
                $"TagName,FailureValue,Occurrences,RelativeFrequencyInTag,RelativeFrequencyInReport",
                $"PixelData,aaaaaaaaaaa,2,0.25,0.25",
                $"PixelData,another,2,0.25,0.25",
                $"PixelData,a,4,0.5,0.5",
                $"",
                $"=== pixel full file ===",
                $"TagName,FailureValue,FilePath",
                $"PixelData,aaaaaaaaaaa,foo1.dcm",
                $"PixelData,aaaaaaaaaaa,foo2.dcm",
                $"PixelData,another,foo1.dcm",
                $"PixelData,another,foo2.dcm",
                $"PixelData,a,foo1.dcm",
                $"PixelData,a,foo1.dcm",
                $"PixelData,a,foo2.dcm",
                $"PixelData,a,foo2.dcm",
                $"",
                $"=== pixel word length frequencies file ===",
                $"WordLength,Count,RelativeFrequencyInReport",
                $"1,4,0.5",
                $"2,0,0",
                $"3,0,0",
                $"4,0,0",
                $"5,0,0",
                $"6,0,0",
                $"7,2,0.25",
                $"8,0,0",
                $"9,0,0",
                $"10,0,0",
                $"11,2,0.25",
                $"",
                $"=== tag summary file ===",
                $"TagName,FailureValue,Occurrences,RelativeFrequencyInTag,RelativeFrequencyInReport",
                $"X,foo,4,0.6666666666666666,0.5",
                $"X,bar,2,0.3333333333333333,0.5",
                $"Z,bar,2,1,0.5",
                $"",
                $"=== tag full file ===",
                $"TagName,FailureValue,FilePath",
                $"X,foo,foo1.dcm",
                $"X,foo,foo1.dcm",
                $"X,foo,foo2.dcm",
                $"X,foo,foo2.dcm",
                $"X,bar,foo1.dcm",
                $"X,bar,foo2.dcm",
                $"Z,bar,foo1.dcm",
                $"Z,bar,foo2.dcm",
                $"",
            };

            Assert.AreEqual(string.Join(newLine, expected), reporter.Report);
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
