using Microservices.CohortExtractor.Execution.ProjectPathResolvers;
using Microservices.CohortExtractor.Execution.RequestFulfillers;
using NUnit.Framework;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using System.IO;


namespace Microservices.CohortExtractor.Tests
{
    public class DefaultProjectPathResolverTest
    {
        #region Fixture Methods

        private ExtractionRequestMessage _requestMessage;

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
        public void TestDefaultProjectPathResolver_IdParts(string study, string series)
        {
            var result = new QueryToExecuteResult(
                "foo.dcm",
                study,
                series,
                "sop",
                false,
                null);

            Assert.AreEqual(
                Path.Combine(
                    study ?? "unknown",
                    series ?? "unknown",
                    "foo-an.dcm"),
                new DefaultProjectPathResolver().GetOutputPath(result, _requestMessage));
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

            Assert.AreEqual(
                Path.Combine(
                    "study",
                    "series",
                    expectedOutput),
                new DefaultProjectPathResolver().GetOutputPath(result, _requestMessage));
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

            Assert.AreEqual(
                Path.Combine(
                    "study",
                    "unknown",
                    "file-an.dcm"),
                new DefaultProjectPathResolver().GetOutputPath(result, _requestMessage));
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

            Assert.AreEqual(
                Path.Combine(
                    "study",
                    "unknown",
                    "file.dcm"),
                new DefaultProjectPathResolver().GetOutputPath(result, requestMessage));
        }

        #endregion
    }
}
