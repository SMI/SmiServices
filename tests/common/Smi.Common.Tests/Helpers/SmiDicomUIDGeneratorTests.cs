using NUnit.Framework;
using Smi.Common.Helpers;
using System.Linq;
using System.Text;

namespace Smi.Common.Tests.Helpers
{
    public class SmiDicomUIDGeneratorTests
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
        public void Generate_HasExpectedPrefix()
        {
            var uid = SmiDicomUIDGenerator.Generate();

            Assert.True(uid.StartsWith("2.25.837773."));
        }

        [Test]
        public void Generate_HasExpectedLength()
        {
            var uid = SmiDicomUIDGenerator.Generate();

            Assert.AreEqual(64, Encoding.ASCII.GetByteCount(uid));
        }

        [Test]
        public void Generate_HasExpectedCharacters()
        {
            var uid = SmiDicomUIDGenerator.Generate();

            var postfix = uid["2.25.837773.".Length..];
            Assert.True(postfix.All(char.IsDigit));
        }


        #endregion
    }
}
