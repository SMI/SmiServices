using NUnit.Framework;
using Smi.Common.Messages.Extraction;

namespace Smi.Common.Tests.Messages
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

            Assert.AreEqual("foo.dcm", statusMessage.DicomFilePath);
            Assert.AreEqual("foo-an.dcm", statusMessage.OutputFilePath);
        }

        #endregion
    }
}
