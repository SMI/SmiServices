using NUnit.Framework;
using SmiServices.Common.Options;

namespace SmiServices.IntegrationTests.Microservices.DicomTagReader
{
    [RequiresRabbit]
    public class DicomTagReaderTests
    {
        [Test]
        public void Main_RunSingleFile_Exception_ReturnsOne()
        {
            // Arrange

            SmiCliInit.InitSmiLogging = false;

            // Act

            var rc = SmiServices.Microservices.DicomTagReader.DicomTagReader.Main(["-f", "some.dcm"]);

            // Assert

            Assert.That(rc, Is.EqualTo(1));
        }
    }
}
