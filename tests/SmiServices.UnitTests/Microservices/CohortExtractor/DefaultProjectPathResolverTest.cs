using NUnit.Framework;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System.IO;


namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class DefaultProjectPathResolverTest
    {
        #region Fixture Methods

        private ExtractionRequestMessage _requestMessage = null!;

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
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [TestCase("study", "series")]
        [TestCase("study", null)]
        [TestCase(null, "series")]
        [TestCase(null, null)]
        public void TestDefaultProjectPathResolver_IdParts(string? study, string? series)
        {
            var result = new QueryToExecuteResult(
                "foo.dcm",
                study,
                series,
                "sop",
                false,
                null);

            Assert.That(
                new DefaultProjectPathResolver().GetOutputPath(result, _requestMessage), Is.EqualTo(Path.Combine(
                    study ?? "unknown",
                    series ?? "unknown",
                    "foo-an.dcm")));
        }

        [TestCase("file-an.dcm", "file.dcm")]
        [TestCase("file-an.dcm", "file.dicom")]
        [TestCase("file-an.dcm", "file")]
        [TestCase("file.foo-an.dcm", "file.foo")]
        public void TestDefaultProjectPathResolver_Extensions(string expectedOutput, string inputFile)
        {
            var result = new QueryToExecuteResult(
                Path.Combine("foo", inputFile),
                "study",
                "series",
                "sop",
                false,
                null);

            Assert.That(
                new DefaultProjectPathResolver().GetOutputPath(result, _requestMessage), Is.EqualTo(Path.Combine(
                    "study",
                    "series",
                    expectedOutput)));
        }

        [Test]
        public void TestDefaultProjectPathResolver_Both()
        {
            var result = new QueryToExecuteResult(
                Path.Combine("foo", "file"),
                "study",
                null,
                "sop",
                false,
                null);

            Assert.That(
                new DefaultProjectPathResolver().GetOutputPath(result, _requestMessage), Is.EqualTo(Path.Combine(
                    "study",
                    "unknown",
                    "file-an.dcm")));
        }

        [Test]
        public void Test_DefaultProjectPathResolver_IdentExtraction()
        {
            var requestMessage = new ExtractionRequestMessage
            {
                IsIdentifiableExtraction = true,
            };

            var result = new QueryToExecuteResult(
                Path.Combine("foo", "file"),
                "study",
                null,
                "sop",
                false,
                null);

            Assert.That(
                new DefaultProjectPathResolver().GetOutputPath(result, requestMessage), Is.EqualTo(Path.Combine(
                    "study",
                    "unknown",
                    "file.dcm")));
        }

        [TestCase(".study", "series")]
        [TestCase(".study", ".series")]
        [TestCase(null, ".series")]
        [TestCase(".study", null)]
        public void TestDefaultProjectPathResolver_HiddenDirectories(string? study, string? series)
        {
            var result = new QueryToExecuteResult(
                "foo.dcm",
                study,
                series,
                "sop",
                false,
                null);

            Assert.That(
                new DefaultProjectPathResolver().GetOutputPath(result, _requestMessage), Is.EqualTo(Path.Combine(
                    study?.TrimStart('.') ?? "unknown",
                    series?.TrimStart('.') ?? "unknown",
                    "foo-an.dcm")));
        }

        #endregion
    }
}
