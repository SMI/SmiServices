using NUnit.Framework;

namespace SmiServices.UnitTests.Common
{
    /// <summary>
    /// Template for test classes. The test class name should match the class under test + 'Test', e.g. 'FooTests'. The test project layout should match the source project layout wherever possible, e.g.:
    /// FooProj
    /// -  FooClass.cs
    /// -  BarDirectory
    ///    -   BarClass.cs
    /// FooProj.Test
    /// -  FooClassTest.cs
    /// -  BarDirectory
    ///    -   BarClassTest.cs
    /// </summary>
    public class TestClassTemplate
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

        /// <summary>
        /// Test names should concisely describe what is being tested and what the expected result is, e.g.
        /// -   MethodName_ReturnsFoo_WhenBar
        /// -   MethodName_ThrowsException_OnInvalidBar
        /// </summary>
        [Test]
        public void TestTemplate() { }

        #endregion
    }
}
