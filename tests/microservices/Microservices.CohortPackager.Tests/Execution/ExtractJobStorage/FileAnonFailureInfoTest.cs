using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
using Smi.Common.Messages.Extraction;
using Smi.Common.Tests;
using System;


namespace Microservices.CohortPackager.Tests.Execution.ExtractJobStorage
{
    public class FileAnonFailureInfoTest
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

        [TestCase(null, ExtractedFileStatus.ErrorWontRetry, "bar", "dicomFilePath")]
        [TestCase("  ", ExtractedFileStatus.ErrorWontRetry, "bar", "dicomFilePath")]
        [TestCase("foo", ExtractedFileStatus.None, "bar", "status")]
        [TestCase("foo", ExtractedFileStatus.Anonymised, "bar", "status")]
        [TestCase("foo", ExtractedFileStatus.ErrorWontRetry, null, "statusMessage")]
        [TestCase("foo", ExtractedFileStatus.ErrorWontRetry, "  ", "statusMessage")]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs(string dicomFilePath, ExtractedFileStatus status, string statusMessage, string expected)
        {
            var exc = Assert.Throws<ArgumentException>(() => { var _ = new FileAnonFailureInfo(dicomFilePath, status, statusMessage); });
            Assert.True(exc.Message.Contains(expected));
        }

        #endregion
    }
}
