using Moq;
using NUnit.Framework;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.Microservices.CohortPackager.JobProcessing.Reporting;
using SmiServices.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;

namespace SmiServices.UnitTests.Microservices.CohortPackager.Execution.JobProcessing.Reporting;

[TestFixture("\r\n")] // Windows
[TestFixture("\n")] // Unix
internal class JobReporterTests
{
    private static readonly TestDateTimeProvider _dateTimeProvider = new();
    private static MockFileSystem _mockFileSystem = new();
    private static string _extractionRoot = "";
    private readonly string _newLine;

    public JobReporterTests(string newLine)
    {
        _newLine = newLine;
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }

    [SetUp]
    public void SetUp()
    {
        _mockFileSystem = new MockFileSystem();
        _extractionRoot = _mockFileSystem.Path.GetFullPath("extraction-root");
        _mockFileSystem.AddDirectory(_extractionRoot);
    }

    [TearDown]
    public void TearDown() { }

    private static CompletedExtractJobInfo TestJobInfo(bool isIdentifiableExtraction = false, bool isNoFilterExtraction = false)
        => new(
            Guid.Parse("d0b6d98f-8c98-4ddb-b469-a6fa7b99dea0"),
            _dateTimeProvider.UtcNow(),
            _dateTimeProvider.UtcNow() + TimeSpan.FromHours(1),
            "1234-5678",
            _mockFileSystem.Path.Combine("1234-5678", "extractions", "test-1"),
            "SeriesInstanceUID",
            123,
            "testUser",
            null,
            isIdentifiableExtraction,
            isNoFilterExtraction
        );

    [Test]
    public void CreateReports_AnonymisedExtraction_Empty()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo();

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns([]);

        var reportsDir = _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1");
        var readmePath = _mockFileSystem.Path.Combine(reportsDir, "README.md");
        var verificationFailuresPath = _mockFileSystem.Path.Combine(reportsDir, "verification_failures.csv");
        var processingErrorsPath = _mockFileSystem.Path.Combine(reportsDir, "processing_errors.csv");
        var rejectedFilesPath = _mockFileSystem.Path.Combine(reportsDir, "rejected_files.csv");

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, _newLine);

        // Act

        reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        Assert.Multiple(() =>
        {
            Assert.That(
                _mockFileSystem.File.ReadAllText(readmePath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    $"# SMI extraction validation report for 1234-5678 test-1",
                    $"",
                    $"Job info:",
                    $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job duration:                 01:00:00",
                    $"-   Job extraction id:            d0b6d98f-8c98-4ddb-b469-a6fa7b99dea0",
                    $"-   Extraction tag:               SeriesInstanceUID",
                    $"-   Extraction modality:          Unspecified",
                    $"-   Requested identifier count:   123",
                    $"-   User name:                    testUser",
                    $"-   Identifiable extraction:      No",
                    $"-   Filtered extraction:          Yes",
                    $"",
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(verificationFailuresPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets",
                    "",
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(processingErrorsPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "DicomFilePath,Reason",
                    ""
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(rejectedFilesPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "ExtractionKey,Count,Reason",
                    ""
                })));
        });
    }

    [Test]
    public void CreateReports_AnonymisedExtraction_WithData()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo();

        var jobRejections = new List<ExtractionIdentifierRejectionInfo>
        {
            new("1.2.3.4", new Dictionary<string, int>
            {
                {"Some error", 123 },
            })
        };

        var anonFailures = new List<FileAnonFailureInfo>
        {
            new("1/2/3.dcm", "Corrupt file"),
        };

        var missingFiles = new List<string>
        {
            "1/2/missing.dcm",
        };

        var verificationFailures = new List<FileVerificationFailureInfo>
        {
            new("1/2/3-an.dcm", "[{'Parts': [{'Classification': 3, 'Offset': 0, 'Word': 'FOO'}], 'Resource': '/foo1.dcm', 'ResourcePrimaryKey': '1.2.3.4', 'ProblemField': 'ScanOptions', 'ProblemValue': 'FOO'}]"),
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(jobRejections);
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(anonFailures);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns(missingFiles);
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

        var reportsDir = _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1");
        var readmePath = _mockFileSystem.Path.Combine(reportsDir, "README.md");
        var verificationFailuresPath = _mockFileSystem.Path.Combine(reportsDir, "verification_failures.csv");
        var processingErrorsPath = _mockFileSystem.Path.Combine(reportsDir, "processing_errors.csv");
        var rejectedFilesPath = _mockFileSystem.Path.Combine(reportsDir, "rejected_files.csv");

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, _newLine);

        // Act

        reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        Assert.Multiple(() =>
        {
            Assert.That(
                _mockFileSystem.File.ReadAllText(readmePath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    $"# SMI extraction validation report for 1234-5678 test-1",
                    $"",
                    $"Job info:",
                    $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job duration:                 01:00:00",
                    $"-   Job extraction id:            d0b6d98f-8c98-4ddb-b469-a6fa7b99dea0",
                    $"-   Extraction tag:               SeriesInstanceUID",
                    $"-   Extraction modality:          Unspecified",
                    $"-   Requested identifier count:   123",
                    $"-   User name:                    testUser",
                    $"-   Identifiable extraction:      No",
                    $"-   Filtered extraction:          Yes",
                    $"",
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(verificationFailuresPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets",
                    "1/2/3-an.dcm,1.2.3.4,ScanOptions,FOO,FOO,Person,0",
                    "",
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(processingErrorsPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "DicomFilePath,Reason",
                    "1/2/missing.dcm,Missing",
                    "1/2/3.dcm,Corrupt file",
                    ""
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(rejectedFilesPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "ExtractionKey,Count,Reason",
                    "1.2.3.4,123,Some error",
                    ""
                })));
        });
    }

    [Test]
    public void CreateReports_IdentifiableExtraction_Empty()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo(isIdentifiableExtraction: true);

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns([]);

        var reportsDir = _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1");
        var readmePath = _mockFileSystem.Path.Combine(reportsDir, "README.md");
        var processingErrorsPath = _mockFileSystem.Path.Combine(reportsDir, "processing_errors.csv");
        var rejectedFilesPath = _mockFileSystem.Path.Combine(reportsDir, "rejected_files.csv");

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, _newLine);

        // Act

        reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        Assert.Multiple(() =>
        {
            Assert.That(
                _mockFileSystem.File.ReadAllText(readmePath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    $"# SMI extraction validation report for 1234-5678 test-1",
                    $"",
                    $"Job info:",
                    $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job duration:                 01:00:00",
                    $"-   Job extraction id:            d0b6d98f-8c98-4ddb-b469-a6fa7b99dea0",
                    $"-   Extraction tag:               SeriesInstanceUID",
                    $"-   Extraction modality:          Unspecified",
                    $"-   Requested identifier count:   123",
                    $"-   User name:                    testUser",
                    $"-   Identifiable extraction:      Yes",
                    $"-   Filtered extraction:          Yes",
                    $"",
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(processingErrorsPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "DicomFilePath,Reason",
                    ""
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(rejectedFilesPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "ExtractionKey,Count,Reason",
                    ""
                })));
        });
    }

    [Test]
    public void CreateReports_IdentifiableExtraction_WithData()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo(isIdentifiableExtraction: true);

        var jobRejections = new List<ExtractionIdentifierRejectionInfo>
        {
            new("1.2.3.4", new Dictionary<string, int>
            {
                {"Some error", 123 },
            })
        };

        var missingFiles = new List<string>
        {
            "1/2/missing.dcm",
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(jobRejections);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns(missingFiles);

        var reportsDir = _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1");
        var readmePath = _mockFileSystem.Path.Combine(reportsDir, "README.md");
        var processingErrorsPath = _mockFileSystem.Path.Combine(reportsDir, "processing_errors.csv");
        var rejectedFilesPath = _mockFileSystem.Path.Combine(reportsDir, "rejected_files.csv");

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, _newLine);

        // Act

        reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        Assert.Multiple(() =>
        {
            Assert.That(
                _mockFileSystem.File.ReadAllText(readmePath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    $"# SMI extraction validation report for 1234-5678 test-1",
                    $"",
                    $"Job info:",
                    $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job duration:                 01:00:00",
                    $"-   Job extraction id:            d0b6d98f-8c98-4ddb-b469-a6fa7b99dea0",
                    $"-   Extraction tag:               SeriesInstanceUID",
                    $"-   Extraction modality:          Unspecified",
                    $"-   Requested identifier count:   123",
                    $"-   User name:                    testUser",
                    $"-   Identifiable extraction:      Yes",
                    $"-   Filtered extraction:          Yes",
                    $"",
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(processingErrorsPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "DicomFilePath,Reason",
                    "1/2/missing.dcm,Missing",
                    ""
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(rejectedFilesPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "ExtractionKey,Count,Reason",
                    "1.2.3.4,123,Some error",
                    ""
                })));
        });
    }

    [Test]
    public void CreateReports_NoFilterExtraction_Empty()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo(isNoFilterExtraction: true);

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns([]);

        var reportsDir = _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1");
        var readmePath = _mockFileSystem.Path.Combine(reportsDir, "README.md");
        var processingErrorsPath = _mockFileSystem.Path.Combine(reportsDir, "processing_errors.csv");
        var rejectedFilesPath = _mockFileSystem.Path.Combine(reportsDir, "rejected_files.csv");
        var verificationFailuresPath = _mockFileSystem.Path.Combine(reportsDir, "verification_failures.csv");

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, _newLine);

        // Act

        reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        Assert.Multiple(() =>
        {
            Assert.That(
                _mockFileSystem.File.ReadAllText(readmePath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    $"# SMI extraction validation report for 1234-5678 test-1",
                    $"",
                    $"Job info:",
                    $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job duration:                 01:00:00",
                    $"-   Job extraction id:            d0b6d98f-8c98-4ddb-b469-a6fa7b99dea0",
                    $"-   Extraction tag:               SeriesInstanceUID",
                    $"-   Extraction modality:          Unspecified",
                    $"-   Requested identifier count:   123",
                    $"-   User name:                    testUser",
                    $"-   Identifiable extraction:      No",
                    $"-   Filtered extraction:          No",
                    $"",
                })));

            Assert.That(
               _mockFileSystem.File.ReadAllText(verificationFailuresPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
               {
                    "Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets",
                    "",
               })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(processingErrorsPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "DicomFilePath,Reason",
                    ""
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(rejectedFilesPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "ExtractionKey,Count,Reason",
                    ""
                })));
        });
    }

    [Test]
    public void CreateReports_NoFilterExtraction_WithData()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo(isNoFilterExtraction: true);

        var jobRejections = new List<ExtractionIdentifierRejectionInfo>
        {
            new("1.2.3.4", new Dictionary<string, int>
            {
                {"Some error", 123 },
            })
        };

        var missingFiles = new List<string>
        {
            "1/2/missing.dcm",
        };

        var anonFailures = new List<FileAnonFailureInfo>
        {
            new("1/2/3.dcm", "Corrupt file"),
        };

        var verificationFailures = new List<FileVerificationFailureInfo>
        {
            new("1/2/3-an.dcm", "[{'Parts': [{'Classification': 3, 'Offset': 0, 'Word': 'FOO'}], 'Resource': '/foo1.dcm', 'ResourcePrimaryKey': '1.2.3.4', 'ProblemField': 'ScanOptions', 'ProblemValue': 'FOO'}]"),
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns(jobRejections);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns(missingFiles);
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns(anonFailures);
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

        var reportsDir = _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1");
        var readmePath = _mockFileSystem.Path.Combine(reportsDir, "README.md");
        var processingErrorsPath = _mockFileSystem.Path.Combine(reportsDir, "processing_errors.csv");
        var rejectedFilesPath = _mockFileSystem.Path.Combine(reportsDir, "rejected_files.csv");
        var verificationFailuresPath = _mockFileSystem.Path.Combine(reportsDir, "verification_failures.csv");

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, _newLine);

        // Act

        reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        Assert.Multiple(() =>
        {
            Assert.That(
                _mockFileSystem.File.ReadAllText(readmePath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    $"# SMI extraction validation report for 1234-5678 test-1",
                    $"",
                    $"Job info:",
                    $"-   Job submitted at:             {_dateTimeProvider.UtcNow().ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job completed at:             {(_dateTimeProvider.UtcNow() + TimeSpan.FromHours(1)).ToString("s", CultureInfo.InvariantCulture)}",
                    $"-   Job duration:                 01:00:00",
                    $"-   Job extraction id:            d0b6d98f-8c98-4ddb-b469-a6fa7b99dea0",
                    $"-   Extraction tag:               SeriesInstanceUID",
                    $"-   Extraction modality:          Unspecified",
                    $"-   Requested identifier count:   123",
                    $"-   User name:                    testUser",
                    $"-   Identifiable extraction:      No",
                    $"-   Filtered extraction:          No",
                    $"",
                })));

            Assert.That(
               _mockFileSystem.File.ReadAllText(verificationFailuresPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "Resource,ResourcePrimaryKey,ProblemField,ProblemValue,PartWords,PartClassifications,PartOffsets",
                    "1/2/3-an.dcm,1.2.3.4,ScanOptions,FOO,FOO,Person,0",
                    "",
               })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(processingErrorsPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "DicomFilePath,Reason",
                    "1/2/missing.dcm,Missing",
                    "1/2/3.dcm,Corrupt file",
                    ""
                })));

            Assert.That(
                _mockFileSystem.File.ReadAllText(rejectedFilesPath)
, Is.EqualTo(string.Join(_newLine, new List<string>
                {
                    "ExtractionKey,Count,Reason",
                    "1.2.3.4,123,Some error",
                    ""
                })));
        });
    }

    [Test]
    public void Constructor_NoNewLine_SetToEnvironment()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo();

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns([]);

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, null);

        // Act

        reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        var readmeText = _mockFileSystem.File.ReadAllText(
            _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1", "README.md")
        );

        Assert.That(readmeText, Does.EndWith(Environment.NewLine));
    }

    [Test]
    public void ReportNewLine_NonNewLine_ThrowsException()
    {
        // Arrange

        var newLine = "\\n";

        // Act

        JobReporter constructor() => new(new Mock<IExtractJobStore>().Object, _mockFileSystem, _extractionRoot, newLine);

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => constructor());
        Assert.That(exc!.Message, Is.EqualTo("Must be a Unix or Windows newline (Parameter 'reportNewLine')"));
    }

    [Test]

    public void Constructor_NonRootedExtractionRoot_ThrowsException()
    {
        // Arrange

        var extractionRoot = "foo/bar";

        // Act

        JobReporter constructor() => new(new Mock<IExtractJobStore>().Object, _mockFileSystem, extractionRoot, "\n");

        // Assert

        var exc = Assert.Throws<ArgumentException>(() => constructor());
        Assert.That(exc!.Message, Is.EqualTo("Path must be rooted (Parameter 'extractionRoot')"));
    }

    [Test]
    public void CreateReports_DefaultJobId_ThrowsException()
    {
        // Arrange

        var jobId = Guid.Empty;
        var reporter = new JobReporter(new Mock<IExtractJobStore>().Object, _mockFileSystem, _extractionRoot, "\n");

        // Act

        void call() => reporter.CreateReports(jobId);

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(call);
        Assert.That(exc!.Message, Is.EqualTo("Must provide a non-zero jobId (Parameter 'jobId')"));
    }

    [Test]
    public void CreateReports_ExistingReportsDir_ThrowsException()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo();

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, "\n");

        var reportsDir = _mockFileSystem.Path.Combine("extraction-root", "1234-5678", "extractions", "reports", "test-1");
        var directoryInfo = _mockFileSystem.Directory.CreateDirectory(reportsDir);

        // Act

        void call() => reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        var exc = Assert.Throws<ApplicationException>(call);
        Assert.That(exc!.Message, Is.EqualTo($"Job reports directory already exists: {directoryInfo.FullName}"));
    }

    [Test]
    public void CreateReports_DeserializeInvalidFailure_ThrowsException()
    {
        // Arrange

        CompletedExtractJobInfo jobInfo = TestJobInfo();

        var verificationFailures = new List<FileVerificationFailureInfo>
        {
            new("1/2/3-an.dcm", "not a failure"),
        };

        var mockJobStore = new Mock<IExtractJobStore>(MockBehavior.Strict);
        mockJobStore.Setup(x => x.GetCompletedJobInfo(It.IsAny<Guid>())).Returns(jobInfo);
        mockJobStore.Setup(x => x.GetCompletedJobRejections(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobAnonymisationFailures(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobMissingFileList(It.IsAny<Guid>())).Returns([]);
        mockJobStore.Setup(x => x.GetCompletedJobVerificationFailures(It.IsAny<Guid>())).Returns(verificationFailures);

        var reporter = new JobReporter(mockJobStore.Object, _mockFileSystem, _extractionRoot, _newLine);

        // Act

        void call() => reporter.CreateReports(jobInfo.ExtractionJobIdentifier);

        // Assert

        var exc = Assert.Throws<ApplicationException>(call);
        Assert.That(exc!.Message, Is.EqualTo("Could not deserialize report content for 1/2/3-an.dcm"));
    }
}
