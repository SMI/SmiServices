using NUnit.Framework;
using Smi.Common.Helpers;
using System;
using System.Linq;
using System.Text;

namespace Smi.Common.Tests.Helpers
{
    public class SmiDicomUIDGeneratorTests
    {
        #region Fixture Methods

        private string _prefix = "54321";
        private SmiDicomUIDGenerator _generator;

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
        public void SetUp()
        {
            _generator = new SmiDicomUIDGenerator(_prefix);
        }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [TestCase("foo")]
        [TestCase("123.456")]
        public void Constructor_NonDigit_ThrowsArgumentException(string prefix)
        {
            var exc = Assert.Throws<ArgumentException>(() => { new SmiDicomUIDGenerator(prefix); });
            Assert.AreEqual("Specified prefix must only contain digits", exc.Message);
        }

        [Test]
        public void Constructor_PrefixTooLong_ThrowsArgumentException()
        {
            // -1 to account for extra '.' after supplied prefix
            var prefix = new string('1', SmiDicomUIDGenerator.DICOM_UID_MAX_LENGTH - SmiDicomUIDGenerator.DICOM_DERIVED_UID_PREFIX.Length - 1);
            var exc = Assert.Throws<ArgumentException>(() => { new SmiDicomUIDGenerator(prefix); });
            Assert.AreEqual("Specified prefix is too long", exc.Message);
        }

        [Test]
        public void Generate_HasExpectedPrefix()
        {
            var uid = _generator.Generate();

            Assert.True(uid.StartsWith($"2.25.{_prefix}."));
        }

        [Test]
        public void Generate_HasExpectedLength()
        {
            var uid = _generator.Generate();

            Assert.AreEqual(64, Encoding.ASCII.GetByteCount(uid));
        }

        [Test]
        public void Generate_HasExpectedCharacters()
        {
            var uid = _generator.Generate();

            var postfix = uid[$"{SmiDicomUIDGenerator.DICOM_DERIVED_UID_PREFIX}{_prefix}.".Length..];
            Assert.True(postfix.All(char.IsDigit));
        }

        #endregion
    }
}
