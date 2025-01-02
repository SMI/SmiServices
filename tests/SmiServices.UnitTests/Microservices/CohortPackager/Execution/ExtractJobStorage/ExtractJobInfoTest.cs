using NUnit.Framework;
using SmiServices.Microservices.CohortPackager.ExtractJobStorage;
using SmiServices.UnitTests.Common;
using System;

namespace SmiServices.UnitTests.Microservices.CohortPackager.Execution.ExtractJobStorage;

public class ExtractJobInfoTest
{
    private readonly TestDateTimeProvider _dateTimeProvider = new();

    #region Fixture Methods 

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
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

    [TestCase("proj/foo/extract-name")]
    [TestCase("proj\\foo\\extract-name")]
    public void Test_ExtractJobInfo_ExtractionName(string extractionDir)
    {
        var info = new ExtractJobInfo(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "1234",
            extractionDir,
            "KeyTag",
            123,
            "testUser",
            "MR",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: false,
            isNoFilterExtraction: false
        );

        Assert.That(info.ExtractionName(), Is.EqualTo("extract-name"));
    }

    [TestCase("proj/foo/extract-name", "proj/foo")]
    [TestCase("proj\\foo\\extract-name", "proj\\foo")]
    public void Test_ExtractJobInfo_ProjectExtractionDir(string extractionDir, string expected)
    {
        var info = new ExtractJobInfo(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "1234",
            extractionDir,
            "KeyTag",
            123,
            "testUser",
            "MR",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: false,
            isNoFilterExtraction: false
        );

        Assert.That(info.ProjectExtractionDir(), Is.EqualTo(expected));
    }


    [Test]
    public void TestExtractJobInfo_Equality()
    {
        Guid guid = Guid.NewGuid();
        var info1 = new ExtractJobInfo(
            guid,
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            "MR",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
            );
        var info2 = new ExtractJobInfo(
            guid,
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            "MR",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
            );

        Assert.That(info2, Is.EqualTo(info1));
    }

    [Test]
    public void TestExtractJobInfo_GetHashCode()
    {
        Guid guid = Guid.NewGuid();
        var info1 = new ExtractJobInfo(
            guid,
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            "MR",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
            );
        var info2 = new ExtractJobInfo(
            guid,
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            "MR",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
            );

        Assert.That(info2.GetHashCode(), Is.EqualTo(info1.GetHashCode()));
    }

    [Test]
    public void Constructor_DefaultExtractionJobIdentifier_ThrowsException()
    {
        // Arrange
        var jobId = Guid.Empty;

        // Act

        ExtractJobInfo call() => new(
            jobId,
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            "MR",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
        );

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
        Assert.That(exc!.Message, Is.EqualTo("Must not be the default Guid (Parameter 'extractionJobIdentifier')"));
    }

    [Test]
    public void Constructor_InvalidModality_ThrowsException()
    {
        // Arrange

        var modality = " ";

        // Act

        ExtractJobInfo call() => new(
            Guid.NewGuid(),
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            modality,
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
        );

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
        Assert.That(exc!.Message, Is.EqualTo("Must not be whitespace if passed (Parameter 'extractionModality')"));
    }

    [Test]
    public void Constructor_DefaultJobSubmittedAt_ThrowsException()
    {
        // Arrange

        var jobSubmittedAt = default(DateTime);

        // Act

        ExtractJobInfo call() => new(
            Guid.NewGuid(),
            jobSubmittedAt,
            "1234",
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            "CT",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
        );

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
        Assert.That(exc!.Message, Is.EqualTo("Must not be the default DateTime (Parameter 'jobSubmittedAt')"));
    }

    [Test]
    public void Constructor_InvalidProjectNumber_ThrowsException()
    {
        // Arrange

        var projectNumber = " ";

        // Act

        ExtractJobInfo call() => new(
            Guid.NewGuid(),
            _dateTimeProvider.UtcNow(),
            projectNumber,
            "test/directory",
            "KeyTag",
            123,
            "testUser",
            "CT",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
        );

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
        Assert.That(exc!.Message, Is.EqualTo("Must not be null or whitespace (Parameter 'projectNumber')"));
    }

    [Test]
    public void Constructor_InvalidExtractionDirectory_ThrowsException()
    {
        // Arrange

        var extractionDirectory = " ";

        // Act

        ExtractJobInfo call() => new(
            Guid.NewGuid(),
            _dateTimeProvider.UtcNow(),
            "1234",
            extractionDirectory,
            "KeyTag",
            123,
            "testUser",
            "CT",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
        );

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
        Assert.That(exc!.Message, Is.EqualTo("Must not be null or whitespace (Parameter 'extractionDirectory')"));
    }

    [Test]
    public void Constructor_InvalidKeyTag_ThrowsException()
    {
        // Arrange

        var keyTag = " ";

        // Act

        ExtractJobInfo call() => new(
            Guid.NewGuid(),
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            keyTag,
            123,
            "testUser",
            "CT",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
        );

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
        Assert.That(exc!.Message, Is.EqualTo("Must not be null or whitespace (Parameter 'keyTag')"));
    }

    [Test]
    public void Constructor_InvalidKeyValue_ThrowsException()
    {
        // Arrange

        uint keyValue = 0;

        // Act

        ExtractJobInfo call() => new(
            Guid.NewGuid(),
            _dateTimeProvider.UtcNow(),
            "1234",
            "test/directory",
            "KeyTag",
            keyValue,
            "testUser",
            "CT",
            ExtractJobStatus.WaitingForCollectionInfo,
            isIdentifiableExtraction: true,
            isNoFilterExtraction: true
        );

        // Assert

        var exc = Assert.Throws<ArgumentOutOfRangeException>(() => call());
        Assert.That(exc!.Message, Is.EqualTo("Must not be zero (Parameter 'keyValueCount')"));
    }

    #endregion
}
