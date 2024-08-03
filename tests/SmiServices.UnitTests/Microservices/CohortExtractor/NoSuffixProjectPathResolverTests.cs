using NUnit.Framework;
using Smi.Common.Helpers;
using Smi.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using System.IO;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class NoSuffixProjectPathResolverTests : Tests.Common.UnitTests
    {

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
                new NoSuffixProjectPathResolver().GetOutputPath(result, new ExtractionRequestMessage()), Is.EqualTo(Path.Combine(
                    study ?? "unknown",
                    series ?? "unknown",
                    "foo.dcm")));
        }

        [TestCase("file.dcm", "file.dcm")]
        [TestCase("file.dcm", "file.dicom")]
        [TestCase("file.dcm", "file")]
        [TestCase("file.foo.dcm", "file.foo")]
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
                new NoSuffixProjectPathResolver().GetOutputPath(result, new ExtractionRequestMessage()), Is.EqualTo(Path.Combine(
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
                new NoSuffixProjectPathResolver().GetOutputPath(result, new ExtractionRequestMessage()), Is.EqualTo(Path.Combine(
                    "study",
                    "unknown",
                    "file.dcm")));
        }

        [Test]
        public void TestCreatingByReflection()
        {
            var instance = new MicroserviceObjectFactory().CreateInstance<IProjectPathResolver>("Microservices.CohortExtractor.Execution.ProjectPathResolvers.NoSuffixProjectPathResolver", typeof(IProjectPathResolver).Assembly, RepositoryLocator);
            Assert.That(instance, Is.InstanceOf<NoSuffixProjectPathResolver>());
        }
    }
}
