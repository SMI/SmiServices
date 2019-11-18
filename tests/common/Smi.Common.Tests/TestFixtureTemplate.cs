
using NUnit.Framework;

namespace Smi.Common.Tests
{
    /// <summary>
    /// Template for test fixtures
    /// </summary>
    [TestFixture]
    public class TestFixtureTemplate
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
        public void ExampleTest()
        {
            Assert.Pass();
        }

        #endregion
    }
}
