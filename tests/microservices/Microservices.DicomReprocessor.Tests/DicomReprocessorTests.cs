
using NUnit.Framework;


namespace Microservices.DicomReprocessor.Tests
{
    [TestFixture]
    public class DicomReprocessorTests
    {
        #region Fixture Methods 

        [OneTimeSetUp]
        public void OneTimeSetUp() { }

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
        public void FileMessage_MessageHeader_IsPresent()
        {
            Assert.Inconclusive("TODO");
        }

        #endregion
    }
}
