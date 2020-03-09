using NUnit.Framework;
using Smi.Common.Tests;

namespace Microservices.CohortPackager.Tests.Execution.JobProcessing
{
    [TestFixture]
    public class ExtractJobWatcherTest
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
        public void Test()
        {
            Assert.Fail("TODO");
        }

        #endregion
    }
}
