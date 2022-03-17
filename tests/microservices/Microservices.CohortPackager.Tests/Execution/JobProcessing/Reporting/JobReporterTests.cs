using Microservices.CohortPackager.Execution.ExtractJobStorage;
using Microservices.CohortPackager.Execution.JobProcessing.Reporting;
using Moq;
using NUnit.Framework;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

// TODO(rkm 2022-03-17) Add test that unspecified newline matches environment

namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting
{
    // TODO(rkm 2020-11-19) Replace hard-coded JSON reports with Failure objects
    [TestFixture]
    public class JobReporterTests
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


            var reporter = new JobReporter(mockJobStore.Object, new FileSystem(), "foobar", newLine);

            reporter.CreateReports(Guid.Empty);

            // TODO
            Assert.Fail();
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
                new FileAnonFailureInfo("foo1.dcm", ExtractedFileStatus.ErrorWontRetry, "image was corrupt"),
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

            var reporter = new JobReporter(mockJobStore.Object, new FileSystem(), "foobar", newLine);

            reporter.CreateReports(Guid.Empty);


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

            // TODO Assert
            Assert.Fail();
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

            var reporter = new JobReporter(mockJobStore.Object, new FileSystem(), "foobar", LinuxNewLine);

            var e = Assert.Throws<ApplicationException>(() => reporter.CreateReports(Guid.Empty));

            // todo
            Assert.AreEqual(e.Message, "eeee");
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

            var reporter = new JobReporter(mockJobStore.Object, new FileSystem(), "foobar", newLine);

            reporter.CreateReports(Guid.Empty);

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

            // todo
            Assert.Fail();
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

            var reporter = new JobReporter(mockJobStore.Object, new FileSystem(), "foobar", LinuxNewLine);

            reporter.CreateReports(Guid.Empty);

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

            // todo
            Assert.Fail();
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

            var reporter = new JobReporter(mockJobStore.Object, new FileSystem(), "foobar", LinuxNewLine);

            var missingFilesExpected = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("missing.dcm", null),
            };

            // todo
            Assert.Fail();
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

            var reporter = new JobReporter(mockJobStore.Object, new FileSystem(), "foobar", LinuxNewLine);

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

            // TODO
            //Assert.AreEqual(string.Join(newLine, expected), reporter.Report);
            Assert.Fail();
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
            var exc = Assert.Throws<ArgumentException>(() => new JobReporter(new Mock<IExtractJobStore>().Object, new FileSystem(), "unused", newLine));
            Assert.AreEqual("ReportNewLine contained an escaped backslash", exc.Message);
        }
    }

    #endregion
}
