using NUnit.Framework;
using SmiServices.Common.Helpers;
using SmiServices.Common.Messages.Extraction;
using SmiServices.Microservices.CohortExtractor.ProjectPathResolvers;
using SmiServices.Microservices.CohortExtractor.RequestFulfillers;
using SmiServices.UnitTests.Common;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace SmiServices.UnitTests.Microservices.CohortExtractor
{
    public class NoSuffixProjectPathResolverTests
    {
        private IFileSystem _fileSystem;

        [SetUp]
        public void SetUp()
        {
            TestLogger.Setup();
            _fileSystem = new MockFileSystem();
        }

        [Test]
        public void GetOutputPath_Basic()
        {
            // Arrange

            var expectedPath = _fileSystem.Path.Combine("study", "series", "foo.dcm");
            var resolver = new NoSuffixProjectPathResolver(_fileSystem);
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

        [TestCase("file.dcm", "file.dcm")]
        [TestCase("file.dcm", "file.dicom")]
        [TestCase("file.dcm", "file")]
        [TestCase("file.foo.dcm", "file.foo")]
        public void GetOutputPath_Extensions(string expectedOutput, string inputFile)
        {
            // Arrange

            var expectedPath = _fileSystem.Path.Combine("study", "series", expectedOutput);
            var resolver = new NoSuffixProjectPathResolver(_fileSystem);
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
        public void CanBeConstructedByReflection()
        {
            var instance = new MicroserviceObjectFactory().CreateInstance<IProjectPathResolver>("SmiServices.Microservices.CohortExtractor.ProjectPathResolvers.NoSuffixProjectPathResolver", typeof(IProjectPathResolver).Assembly, _fileSystem);
            Assert.That(instance, Is.InstanceOf<NoSuffixProjectPathResolver>());
        }
    }
}
