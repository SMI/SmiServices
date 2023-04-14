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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text.RegularExpressions;


namespace Microservices.CohortPackager.Tests.Execution.JobProcessing.Reporting;

[TestFixture]
public class JobReporterTests
{
    private const string WindowsNewLine = "\r\n";
    private const string LinuxNewLine = "\n";
    private const string ExtractRoot = "exRoot";

    private MockFileSystem _fileSystem;

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
    public void SetUp()
    {
        _fileSystem = new MockFileSystem();
    }

    [TearDown]
    public void TearDown() { }

    private static CompletedExtractJobInfo TestJobInfo(
        IFileSystem fs,
        bool isIdentifiableExtraction = false,
        bool isNoFilterExtraction = false
    ) =>
        new(
            Guid.NewGuid(),
            _dateTimeProvider.UtcNow(),
            _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
            "proj1",
            fs.Path.Combine("proj1", "extractions", "test"),
            "keyTag",
            123,
            null,
            isIdentifiableExtraction,
            isNoFilterExtraction
        );

    private void VerifyReports(
        CompletedExtractJobInfo completedJobInfo,
        string newLine,
        List<string> rejectionsCsvRows,
        List<string> processingErrorsLines,
        List<string> verificationFailureCsvLines,
        List<string> missingFilesLines
    )
    {
        string identExtraction = completedJobInfo.IsIdentifiableExtraction ? "Yes" : "No";
        string filteredExtraction = !completedJobInfo.IsNoFilterExtraction ? "Yes" : "No";
        var expectedLines = new List<string>
        {
            $"# SMI extraction reports for {completedJobInfo.ProjectNumber} - {completedJobInfo.ExtractionName()}",
            $"",
            $"Job info:",
            $"-   Job extraction id:            {completedJobInfo.ExtractionJobIdentifier}",
            $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
            $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
            $"-   Job duration:                 {TimeSpan.FromHours(1)}",
            $"-   Extraction tag:               {completedJobInfo.KeyTag}",
            $"-   Extraction modality:          {completedJobInfo.ExtractionModality ?? "Unspecified"}",
            $"-   Requested identifier count:   {completedJobInfo.KeyValueCount}",
            $"-   Identifiable extraction:      {identExtraction}",
            $"-   Filtered extraction:          {filteredExtraction}",
            $"",
            $"--- END ---",
            $"",
        };

        newLine ??= Environment.NewLine;

        using var readmeStream = _fileSystem.File.OpenRead(_fileSystem.Path.Combine(ExtractRoot, completedJobInfo.ExtractionReportsDir(_fileSystem), "README.md"));
        using (var streamReader = new StreamReader(readmeStream))
        {
            var content = streamReader.ReadToEnd();
            Assert.AreEqual(string.Join(newLine, expectedLines), content);
        }

        if (completedJobInfo.IsIdentifiableExtraction)
            VerifyReports_IdentifiableExtraction(completedJobInfo, newLine, missingFilesLines);
        else
            VerifyReports_NormalExtraction(completedJobInfo, newLine, rejectionsCsvRows, processingErrorsLines, verificationFailureCsvLines);
    }

    private void VerifyReports_NormalExtraction(
        CompletedExtractJobInfo completedJobInfo,
        string newLine,
        List<string> rejectionsCsvRows,
        List<string> processingErrorsLines,
        List<string> verificationFailureCsvLines
    )
    {
        Assert.AreEqual(4, _fileSystem.AllFiles.ToList().Count);

        var expectedLines = new List<string>
        {
            $"RequestedUID,Reason,Count",
            $"",
        };
        expectedLines.InsertRange(1, rejectionsCsvRows ?? new List<string>());
        using var rejectionsCsvStream = _fileSystem.File.OpenRead(_fileSystem.Path.Combine(ExtractRoot, completedJobInfo.ExtractionReportsDir(_fileSystem), "rejections.csv"));
        using (var streamReader = new StreamReader(rejectionsCsvStream))
        {
            var content = streamReader.ReadToEnd();
            Assert.AreEqual(string.Join(newLine, expectedLines), content);
        }

        expectedLines = new List<string>
        {
            $"# SMI extraction processing errors for {completedJobInfo.ProjectNumber} - {completedJobInfo.ExtractionName()}",
            $"",
            $"--- END ---",
            $"",
        };
        expectedLines.InsertRange(2, processingErrorsLines ?? new List<string>());
        using var processingErrorsMdStream = _fileSystem.File.OpenRead(_fileSystem.Path.Combine(ExtractRoot, completedJobInfo.ExtractionReportsDir(_fileSystem), "processing_errors.md"));
        using (var streamReader = new StreamReader(processingErrorsMdStream))
        {
            var content = streamReader.ReadToEnd();
            Assert.AreEqual(string.Join(newLine, expectedLines), content);
        }

        expectedLines = new List<string>
        {
            $"Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets",
            $""
        };
        expectedLines.InsertRange(1, verificationFailureCsvLines ?? new List<string>());
        using var verificationErrorsCsvStream = _fileSystem.File.OpenRead(_fileSystem.Path.Combine(ExtractRoot, completedJobInfo.ExtractionReportsDir(_fileSystem), "verification_failures.csv"));
        using (var streamReader = new StreamReader(verificationErrorsCsvStream))
        {
            var content = streamReader.ReadToEnd();
            Assert.AreEqual(string.Join(newLine, expectedLines), content);
        }
    }

    private void VerifyReports_IdentifiableExtraction(
        CompletedExtractJobInfo completedJobInfo,
        string newLine,
        List<string> missingFilesLines
    )
    {
        Assert.AreEqual(2, _fileSystem.AllFiles.ToList().Count);

        var expectedLines = new List<string>
        {
            $"MissingFilePath",
            $"",
        };
        expectedLines.InsertRange(1, missingFilesLines ?? new List<string>());
        using var verificationErrorsCsvStream = _fileSystem.File.OpenRead(_fileSystem.Path.Combine(ExtractRoot, completedJobInfo.ExtractionReportsDir(_fileSystem), "rejections.csv"));
        using (var streamReader = new StreamReader(verificationErrorsCsvStream))
        {
            var content = streamReader.ReadToEnd();
            Assert.AreEqual(string.Join(newLine, expectedLines), content);
        }
    }

    #endregion

    #region Tests

    [Test]
    public void CreateReport_Empty_DefaultNewLine()
    {
        // Arrange
        CompletedExtractJobInfo completedJobInfo = TestJobInfo(_fileSystem);
        _fileSystem.AddDirectory(_fileSystem.Path.Join(ExtractRoot, completedJobInfo.ExtractionDirectory));

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(completedJobInfo.ExtractionJobIdentifier)).Returns(completedJobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<ExtractionIdentifierRejectionInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<FileAnonFailureInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<FileVerificationFailureInfo>());

        string newLine = null;

        var reporter = new JobReporter(mockJobStore.Object, _fileSystem, ExtractRoot, newLine);

        // Act
        reporter.CreateReports(completedJobInfo.ExtractionJobIdentifier);

        // Assert
        VerifyReports(
            completedJobInfo,
            newLine,
            rejectionsCsvRows: null,
            processingErrorsLines: null,
            verificationFailureCsvLines: null,
            missingFilesLines: null
        );
    }

    [TestCase(LinuxNewLine)]
    [TestCase(WindowsNewLine)]
    public void CreateReport_Empty(string newLine)
    {
        // Arrange
        CompletedExtractJobInfo completedJobInfo = TestJobInfo(_fileSystem);
        _fileSystem.AddDirectory(_fileSystem.Path.Join(ExtractRoot, completedJobInfo.ExtractionDirectory));

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(completedJobInfo.ExtractionJobIdentifier)).Returns(completedJobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<ExtractionIdentifierRejectionInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<FileAnonFailureInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<FileVerificationFailureInfo>());

        var reporter = new JobReporter(mockJobStore.Object, _fileSystem, ExtractRoot, newLine);

        // Act
        reporter.CreateReports(completedJobInfo.ExtractionJobIdentifier);

        // Assert
        VerifyReports(
            completedJobInfo,
            newLine,
            rejectionsCsvRows: null,
            processingErrorsLines: null,
            verificationFailureCsvLines: null,
            missingFilesLines: null
        );
    }

    [TestCase(LinuxNewLine)]
    [TestCase(WindowsNewLine)]
    public void CreateReport_BasicData(string newLine)
    {
        // Arrange
        CompletedExtractJobInfo completedJobInfo = TestJobInfo(_fileSystem);
        _fileSystem.AddDirectory(_fileSystem.Path.Join(ExtractRoot, completedJobInfo.ExtractionDirectory));

        var rejections = new List<ExtractionIdentifierRejectionInfo>
        {
            new ExtractionIdentifierRejectionInfo(
                keyValue: "1.2.3.4",
                new Dictionary<string, int>
                {
                    {"denied", 1},
                    {"foo bar", 2},
                }),
            new ExtractionIdentifierRejectionInfo(
                keyValue: "5.6.7.8",
                new Dictionary<string, int>
                {
                    {"denied", 1},
                }),
        };

        var anonFailures = new List<FileAnonFailureInfo>
        {
            new FileAnonFailureInfo("foo1.dcm", ExtractedFileStatus.FileMissing, "could not find the image"),
            new FileAnonFailureInfo("foo2.dcm", ExtractedFileStatus.ErrorWontRetry, "StackTrace:\nAaah..."),
        };

        const string report = @"
[
    {
        'Parts': [],
        'Resource': '/root/2022/01/01/A/foo1.dcm',
        'ResourcePrimaryKey': '1.2.3.4',
        'ProblemField': 'ScanOptions',
        'ProblemValue': 'FOO'
    }
]";

        var verificationFailures = new List<FileVerificationFailureInfo>
        {
            new FileVerificationFailureInfo("study/series/foo1-an.dcm", report),
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(completedJobInfo.ExtractionJobIdentifier)).Returns(completedJobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(completedJobInfo.ExtractionJobIdentifier)).Returns(rejections);
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(anonFailures);
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(verificationFailures);

        var reporter = new JobReporter(mockJobStore.Object, _fileSystem, ExtractRoot, newLine);

        // Act
        reporter.CreateReports(completedJobInfo.ExtractionJobIdentifier);

        // Assert
        VerifyReports(
            completedJobInfo,
            newLine,
            rejectionsCsvRows: new List<string>
            {
                $"1.2.3.4,denied,1",
                $"1.2.3.4,foo bar,2",
                $"5.6.7.8,denied,1",
            },
            processingErrorsLines: new List<string>
            {
                $"-   foo1.dcm - FileMissing",
                $"    ```console",
                $"    could not find the image",
                $"    ```",
                $"",
                $"-   foo2.dcm - ErrorWontRetry",
                $"    ```console",
                $"    StackTrace:",
                $"    Aaah...",
                $"    ```",
                $"",
            },
            verificationFailureCsvLines: new List<string>
            {
                $"study/series/foo1-an.dcm,1.2.3.4,ScanOptions,FOO,,,",
            },
            missingFilesLines: null
        );
    }

    [TestCase(LinuxNewLine)]
    [TestCase(WindowsNewLine)]
    public void CreateReport_MultilineFailureData(string newLine)
    {
        // Arrange
        CompletedExtractJobInfo completedJobInfo = TestJobInfo(_fileSystem);
        _fileSystem.AddDirectory(_fileSystem.Path.Join(ExtractRoot, completedJobInfo.ExtractionDirectory));

        const string reportFormat = @"
[
    {{
        'Parts': [],
        'Resource': '/root/2022/01/01/A/foo1.dcm',
        'ResourcePrimaryKey': '1.2.3.4',
        'ProblemField': 'TextValue',
        'ProblemValue': 'This is a SR for Mr Foo bar{0}Clinical history: ...{0}Normal appearance of ...{0}Verified by: Dr Baz'
    }}
]";
        string report = string.Format(reportFormat, newLine);

        var verificationFailures = new List<FileVerificationFailureInfo>
        {
            new FileVerificationFailureInfo("study/series/foo1-an.dcm", report),
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(completedJobInfo.ExtractionJobIdentifier)).Returns(completedJobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<ExtractionIdentifierRejectionInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<FileAnonFailureInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(verificationFailures);

        var reporter = new JobReporter(mockJobStore.Object, _fileSystem, ExtractRoot, newLine);

        // Act
        reporter.CreateReports(completedJobInfo.ExtractionJobIdentifier);

        // Assert
        VerifyReports(
            completedJobInfo,
            newLine,
            rejectionsCsvRows: null,
            processingErrorsLines: null,
            verificationFailureCsvLines: new List<string>
            {
                $"study/series/foo1-an.dcm,1.2.3.4,TextValue,\"This is a SR for Mr Foo bar{newLine}Clinical history: ...{newLine}Normal appearance of ...{newLine}Verified by: Dr Baz\",,,",
            },
            missingFilesLines: null
        );
    }

    [TestCase(LinuxNewLine)]
    [TestCase(WindowsNewLine)]
    public void CreateReport_IdentifiableExtraction(string newLine)
    {
        // Arrange
        CompletedExtractJobInfo completedJobInfo = TestJobInfo(_fileSystem, isIdentifiableExtraction: true);
        _fileSystem.AddDirectory(_fileSystem.Path.Join(ExtractRoot, completedJobInfo.ExtractionDirectory));

        var missingFiles = new List<string>
        {
            "path/to/file1.dcm",
            "path/to/file2.dcm",
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(completedJobInfo.ExtractionJobIdentifier)).Returns(completedJobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(completedJobInfo.ExtractionJobIdentifier)).Returns(missingFiles);

        var reporter = new JobReporter(mockJobStore.Object, _fileSystem, ExtractRoot, newLine);

        // Act
        reporter.CreateReports(completedJobInfo.ExtractionJobIdentifier);

        // Assert
        VerifyReports(
            completedJobInfo,
            newLine,
            rejectionsCsvRows: null,
            processingErrorsLines: null,
            verificationFailureCsvLines: null,
            missingFilesLines: new List<string>
            {
                "path/to/file1.dcm",
                "path/to/file2.dcm",
            }
        );
    }

    [Test]
    public void InvalidReport_ThrowsApplicationException()
    {
        // Arrange
        CompletedExtractJobInfo completedJobInfo = TestJobInfo(_fileSystem);
        _fileSystem.AddDirectory(_fileSystem.Path.Join(ExtractRoot, completedJobInfo.ExtractionDirectory));

        var verificationFailures = new List<FileVerificationFailureInfo>
        {
            new FileVerificationFailureInfo(anonFilePath: "foo1.dcm", failureData: "totally not a report"),
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(completedJobInfo.ExtractionJobIdentifier)).Returns(completedJobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<ExtractionIdentifierRejectionInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(new List<FileAnonFailureInfo>());
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(completedJobInfo.ExtractionJobIdentifier)).Returns(verificationFailures);

        var reporter = new JobReporter(mockJobStore.Object, _fileSystem, ExtractRoot, LinuxNewLine);

        // Act
        var e = Assert.Throws<ApplicationException>(() => reporter.CreateReports(completedJobInfo.ExtractionJobIdentifier));

        // Assert
        Assert.AreEqual(e.Message, "Could not deserialize report content to IEnumerable<Failure>");
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
    public void ReportNewline_EscapedString_IsRejected()
    {
        const string newLine = @"\n";
        var exc = Assert.Throws<ArgumentException>(() => new JobReporter(new Mock<IExtractJobStore>().Object, new FileSystem(), "unused", newLine));
        Assert.AreEqual("ReportNewLine contained an escaped backslash", exc.Message);
    }
}

#endregion
