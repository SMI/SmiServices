using NUnit.Framework;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;


namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class StudySeriesOriginalFilenameProjectPathResolverTests
    {
        #region Fixture Methods

        private ExtractionRequestMessage _requestMessage = null!;
        private IFileSystem _fileSystem;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();

            _requestMessage = new ExtractionRequestMessage
            {
                IsIdentifiableExtraction = false,
            };
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

        #endregion

        #region Tests

        [Test]
        public void GetOutputPath_Basic()
        {
            // Arrange

            var expectedPath = _fileSystem.Path.Combine("study", "series", "foo-an.dcm");
            var resolver = new StudySeriesOriginalFilenameProjectPathResolver(_fileSystem);
            var result = new QueryToExecuteResult(
                "foo.dcm",
                "study",
                "series",
                "sop",
                rejection: false,
                rejectionReason: null
            );
            var message = new ExtractionRequestMessage();

            // Act

            var actualPath = resolver.GetOutputPath(result, message);

            // Assert
            Assert.That(actualPath, Is.EqualTo(expectedPath));
        }

        [TestCase("file-an.dcm", "file.dcm")]
        [TestCase("file-an.dcm", "file.dicom")]
        [TestCase("file-an.dcm", "file")]
        [TestCase("file.foo-an.dcm", "file.foo")]
        public void GetOutputPath_Extensions(string expectedOutput, string inputFile)
        {
            // Arrange

            var expectedPath = _fileSystem.Path.Combine("study", "series", expectedOutput);
            var resolver = new StudySeriesOriginalFilenameProjectPathResolver(_fileSystem);
            var result = new QueryToExecuteResult(
                inputFile,
                "study",
                "series",
                "sop",
                rejection: false,
                rejectionReason: null
            );
            var message = new ExtractionRequestMessage();

            // Act

            var actualPath = resolver.GetOutputPath(result, message);

            // Assert
            Assert.That(actualPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void GetOutputPath_IdentExtraction()
        {
            // Arrange

            var expectedPath = _fileSystem.Path.Combine("study", "series", "foo.dcm");
            var resolver = new StudySeriesOriginalFilenameProjectPathResolver(_fileSystem);
            var result = new QueryToExecuteResult(
                "foo.dcm",
                "study",
                "series",
                "sop",
                rejection: false,
                rejectionReason: null
            );
            var message = new ExtractionRequestMessage()
            {
                IsIdentifiableExtraction = true,
            };

            // Act

            var actualPath = resolver.GetOutputPath(result, message);

            // Assert
            Assert.That(actualPath, Is.EqualTo(expectedPath));
        }

        #endregion
    }
}
