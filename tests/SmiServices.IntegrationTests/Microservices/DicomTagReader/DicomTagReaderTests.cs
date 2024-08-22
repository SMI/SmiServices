using NUnit.Framework;
using SmiServices.Common.Options;
using System.IO.Abstractions;

namespace SmiServices.IntegrationTests.Microservices.DicomTagReader
{
    [RequiresRabbit]
    public class DicomTagReaderTests
    {
        [Test]
        public void Main_RunSingleFile_Exception_ReturnsOne()
        {
            // Arrange

            var fileSystem = new FileSystem();
            fileSystem.File.WriteAllLines(
                "foo.yaml",
                [
                    "LoggingOptions:"
                ]
            );

            SmiCliInit.InitSmiLogging = false;

            // Act

            var rc = SmiServices.Microservices.DicomTagReader.DicomTagReader.Main(["-y", "foo.yaml", "-f", "some.dcm"]);

            // Assert

            Assert.That(rc, Is.EqualTo(1));
        }
    }
}
