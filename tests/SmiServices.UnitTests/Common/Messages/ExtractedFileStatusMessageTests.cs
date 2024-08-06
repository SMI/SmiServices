using NUnit.Framework;
using SmiServices.Common.Messages.Extraction;

namespace SmiServices.UnitTests.Common.Messages
{
    public class ExtractedFileStatusMessageTests
    {
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
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void Constructor_FromExtractFileMessage_CopiesFilePath()
        {
            var fileMessage = new ExtractFileMessage
            {
                DicomFilePath = "foo.dcm",
                OutputPath = "foo-an.dcm",
            };

            var statusMessage = new ExtractedFileStatusMessage(fileMessage);

            Assert.Multiple(() =>
            {
                Assert.That(statusMessage.DicomFilePath, Is.EqualTo("foo.dcm"));
                Assert.That(statusMessage.OutputFilePath, Is.EqualTo("foo-an.dcm"));
            });
        }

        #endregion
    }
}
