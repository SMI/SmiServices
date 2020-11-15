using Microservices.CohortPackager.Execution.ExtractJobStorage;
using NUnit.Framework;
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

        [Test]
        public void Constructor_ThrowsArgumentException_OnInvalidArgs()
        {
            Assert.Throws<ArgumentException>(() => { var _ = new FileAnonFailureInfo(null, "bar"); });
            Assert.Throws<ArgumentException>(() => { var _ = new FileAnonFailureInfo("  ", "bar"); });
            Assert.Throws<ArgumentException>(() => { var _ = new FileAnonFailureInfo("foo", null); });
            Assert.Throws<ArgumentException>(() => { var _ = new FileAnonFailureInfo("foo", "  "); });
        }

        #endregion
    }
}
